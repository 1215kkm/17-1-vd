using System.Net.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using BitcoinPredictor.Models;

namespace BitcoinPredictor.Services;

/// <summary>
/// 비트코인 가격 데이터 수집 서비스
/// CoinGecko API 사용 (무료)
/// </summary>
public class BitcoinDataService
{
    private readonly HttpClient _httpClient;
    private const string COINGECKO_API = "https://api.coingecko.com/api/v3";
    private const string CACHE_FILE = "bitcoin_price_cache.json";

    public BitcoinDataService()
    {
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "BitcoinPredictor/1.0");
    }

    /// <summary>
    /// 최근 3년간 비트코인 가격 데이터 가져오기
    /// </summary>
    public async Task<List<BitcoinPrice>> GetHistoricalDataAsync(int days = 1095) // 3년 = 약 1095일
    {
        try
        {
            // 캐시 확인 (24시간 이내면 캐시 사용)
            if (File.Exists(CACHE_FILE))
            {
                var cacheInfo = new FileInfo(CACHE_FILE);
                if ((DateTime.Now - cacheInfo.LastWriteTime).TotalHours < 24)
                {
                    var cachedData = await File.ReadAllTextAsync(CACHE_FILE);
                    var cached = JsonConvert.DeserializeObject<List<BitcoinPrice>>(cachedData);
                    if (cached != null && cached.Count > 0)
                        return cached;
                }
            }

            // CoinGecko API로 데이터 가져오기
            var url = $"{COINGECKO_API}/coins/bitcoin/market_chart?vs_currency=usd&days={days}&interval=daily";
            var response = await _httpClient.GetStringAsync(url);
            var json = JObject.Parse(response);

            var prices = new List<BitcoinPrice>();
            var pricesArray = json["prices"] as JArray;

            if (pricesArray != null)
            {
                for (int i = 0; i < pricesArray.Count; i++)
                {
                    var item = pricesArray[i] as JArray;
                    if (item != null && item.Count >= 2)
                    {
                        var timestamp = (long)item[0]!;
                        var price = (decimal)item[1]!;
                        var date = DateTimeOffset.FromUnixTimeMilliseconds(timestamp).DateTime;

                        // OHLC 데이터가 없으므로 가격으로 대체 (실제로는 다른 API 사용 권장)
                        prices.Add(new BitcoinPrice
                        {
                            Date = date.Date,
                            Open = price,
                            High = price,
                            Low = price,
                            Close = price,
                            Volume = 0
                        });
                    }
                }
            }

            // OHLC 데이터 보완 (Binance API 사용)
            prices = await EnhanceWithOHLCDataAsync(prices);

            // 캐시 저장
            await File.WriteAllTextAsync(CACHE_FILE, JsonConvert.SerializeObject(prices, Formatting.Indented));

            return prices;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"데이터 수집 오류: {ex.Message}");

            // 캐시가 있으면 캐시 반환
            if (File.Exists(CACHE_FILE))
            {
                var cachedData = await File.ReadAllTextAsync(CACHE_FILE);
                return JsonConvert.DeserializeObject<List<BitcoinPrice>>(cachedData) ?? new List<BitcoinPrice>();
            }

            return new List<BitcoinPrice>();
        }
    }

    /// <summary>
    /// Binance API로 OHLC 데이터 보완
    /// </summary>
    private async Task<List<BitcoinPrice>> EnhanceWithOHLCDataAsync(List<BitcoinPrice> prices)
    {
        try
        {
            // Binance에서 일봉 데이터 가져오기 (최대 1000개씩)
            var allKlines = new List<JArray>();
            var endTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            for (int i = 0; i < 4; i++) // 4번 반복하면 약 4000일 (3년 이상)
            {
                var url = $"https://api.binance.com/api/v3/klines?symbol=BTCUSDT&interval=1d&limit=1000&endTime={endTime}";
                var response = await _httpClient.GetStringAsync(url);
                var klines = JArray.Parse(response);

                if (klines.Count == 0) break;

                allKlines.InsertRange(0, klines.Cast<JArray>());

                // 다음 요청을 위해 가장 오래된 데이터의 시간 사용
                var oldestTime = (long)klines[0]![0]!;
                endTime = oldestTime - 1;

                await Task.Delay(100); // API 제한 방지
            }

            // 날짜별 OHLC 매핑
            var ohlcDict = new Dictionary<DateTime, BitcoinPrice>();
            foreach (var kline in allKlines)
            {
                var timestamp = (long)kline[0]!;
                var date = DateTimeOffset.FromUnixTimeMilliseconds(timestamp).DateTime.Date;

                ohlcDict[date] = new BitcoinPrice
                {
                    Date = date,
                    Open = decimal.Parse(kline[1]!.ToString()),
                    High = decimal.Parse(kline[2]!.ToString()),
                    Low = decimal.Parse(kline[3]!.ToString()),
                    Close = decimal.Parse(kline[4]!.ToString()),
                    Volume = decimal.Parse(kline[5]!.ToString())
                };
            }

            // 기존 데이터 업데이트
            for (int i = 0; i < prices.Count; i++)
            {
                if (ohlcDict.TryGetValue(prices[i].Date, out var ohlc))
                {
                    prices[i] = ohlc;
                }
            }

            return prices.OrderBy(p => p.Date).ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"OHLC 데이터 보완 오류: {ex.Message}");
            return prices;
        }
    }

    /// <summary>
    /// 주간 데이터로 변환
    /// </summary>
    public List<WeeklyData> ConvertToWeeklyData(List<BitcoinPrice> dailyPrices)
    {
        var weeklyData = new List<WeeklyData>();

        var grouped = dailyPrices
            .GroupBy(p => new { p.Date.Year, Week = GetWeekOfYear(p.Date) })
            .OrderBy(g => g.Key.Year)
            .ThenBy(g => g.Key.Week);

        foreach (var week in grouped)
        {
            var prices = week.OrderBy(p => p.Date).ToList();
            if (prices.Count == 0) continue;

            var weekData = new WeeklyData
            {
                Year = week.Key.Year,
                WeekNumber = week.Key.Week,
                WeekStart = prices.First().Date,
                WeekEnd = prices.Last().Date,
                OpenPrice = prices.First().Open,
                ClosePrice = prices.Last().Close,
                HighPrice = prices.Max(p => p.High),
                LowPrice = prices.Min(p => p.Low),
                DailyPrices = prices
            };

            weekData.WeeklyChangePercent = weekData.OpenPrice != 0
                ? ((weekData.ClosePrice - weekData.OpenPrice) / weekData.OpenPrice) * 100
                : 0;
            weekData.IsUp = weekData.ClosePrice > weekData.OpenPrice;

            weeklyData.Add(weekData);
        }

        return weeklyData;
    }

    private int GetWeekOfYear(DateTime date)
    {
        var cal = System.Globalization.CultureInfo.CurrentCulture.Calendar;
        return cal.GetWeekOfYear(date,
            System.Globalization.CalendarWeekRule.FirstFourDayWeek,
            DayOfWeek.Monday);
    }

    /// <summary>
    /// 현재 비트코인 가격 가져오기
    /// </summary>
    public async Task<decimal> GetCurrentPriceAsync()
    {
        try
        {
            var url = $"{COINGECKO_API}/simple/price?ids=bitcoin&vs_currencies=usd";
            var response = await _httpClient.GetStringAsync(url);
            var json = JObject.Parse(response);
            return (decimal)json["bitcoin"]!["usd"]!;
        }
        catch
        {
            return 0;
        }
    }
}
