using BitcoinPredictor.Models;

namespace BitcoinPredictor.Services;

/// <summary>
/// 비트코인 가격 패턴 분석 서비스
/// </summary>
public class PatternAnalysisService
{
    /// <summary>
    /// 기술적 지표 계산
    /// </summary>
    public TechnicalIndicators CalculateIndicators(List<BitcoinPrice> prices, int currentIndex)
    {
        var indicators = new TechnicalIndicators();

        if (currentIndex < 99 || prices.Count <= currentIndex)
            return indicators;

        // SMA (단순이동평균) 계산
        indicators.SMA7 = CalculateSMA(prices, currentIndex, 7);
        indicators.SMA25 = CalculateSMA(prices, currentIndex, 25);
        indicators.SMA99 = CalculateSMA(prices, currentIndex, 99);

        // RSI 계산 (14일)
        indicators.RSI = CalculateRSI(prices, currentIndex, 14);

        // MACD 계산
        var (macd, signal) = CalculateMACD(prices, currentIndex);
        indicators.MACD = macd;
        indicators.MACDSignal = signal;

        // 볼린저 밴드 계산 (20일, 2 표준편차)
        var (upper, middle, lower) = CalculateBollingerBands(prices, currentIndex, 20, 2);
        indicators.BollingerUpper = upper;
        indicators.BollingerMiddle = middle;
        indicators.BollingerLower = lower;

        return indicators;
    }

    private decimal CalculateSMA(List<BitcoinPrice> prices, int endIndex, int period)
    {
        if (endIndex < period - 1) return 0;

        var sum = 0m;
        for (int i = endIndex - period + 1; i <= endIndex; i++)
        {
            sum += prices[i].Close;
        }
        return sum / period;
    }

    private decimal CalculateRSI(List<BitcoinPrice> prices, int endIndex, int period)
    {
        if (endIndex < period) return 50;

        var gains = 0m;
        var losses = 0m;

        for (int i = endIndex - period + 1; i <= endIndex; i++)
        {
            var change = prices[i].Close - prices[i - 1].Close;
            if (change > 0) gains += change;
            else losses += Math.Abs(change);
        }

        var avgGain = gains / period;
        var avgLoss = losses / period;

        if (avgLoss == 0) return 100;

        var rs = avgGain / avgLoss;
        return 100 - (100 / (1 + rs));
    }

    private (decimal macd, decimal signal) CalculateMACD(List<BitcoinPrice> prices, int endIndex)
    {
        var ema12 = CalculateEMA(prices, endIndex, 12);
        var ema26 = CalculateEMA(prices, endIndex, 26);
        var macd = ema12 - ema26;

        // MACD 시그널 라인 (9일 EMA)
        // 간단히 최근 9개의 MACD 평균으로 대체
        var signal = macd; // 실제로는 MACD의 EMA를 계산해야 함

        return (macd, signal);
    }

    private decimal CalculateEMA(List<BitcoinPrice> prices, int endIndex, int period)
    {
        if (endIndex < period - 1) return prices[endIndex].Close;

        var multiplier = 2m / (period + 1);
        var ema = CalculateSMA(prices, endIndex - period + 1 + period - 1, period);

        for (int i = endIndex - period + 1; i <= endIndex; i++)
        {
            ema = (prices[i].Close - ema) * multiplier + ema;
        }

        return ema;
    }

    private (decimal upper, decimal middle, decimal lower) CalculateBollingerBands(
        List<BitcoinPrice> prices, int endIndex, int period, decimal stdDevMultiplier)
    {
        if (endIndex < period - 1) return (0, 0, 0);

        var middle = CalculateSMA(prices, endIndex, period);

        var sumSquares = 0m;
        for (int i = endIndex - period + 1; i <= endIndex; i++)
        {
            var diff = prices[i].Close - middle;
            sumSquares += diff * diff;
        }

        var stdDev = (decimal)Math.Sqrt((double)(sumSquares / period));
        var upper = middle + (stdDevMultiplier * stdDev);
        var lower = middle - (stdDevMultiplier * stdDev);

        return (upper, middle, lower);
    }

    /// <summary>
    /// 모든 패턴 분석
    /// </summary>
    public List<PatternAnalysis> AnalyzeAllPatterns(List<BitcoinPrice> prices, List<WeeklyData> weeklyData)
    {
        var patterns = new List<PatternAnalysis>();

        // 1. 요일별 패턴
        patterns.Add(AnalyzeDayOfWeekPattern(prices));

        // 2. 월별 패턴
        patterns.Add(AnalyzeMonthPattern(prices));

        // 3. 이동평균선 교차 패턴
        patterns.Add(AnalyzeMAPattern(prices));

        // 4. RSI 과매수/과매도 패턴
        patterns.Add(AnalyzeRSIPattern(prices));

        // 5. 볼린저 밴드 패턴
        patterns.Add(AnalyzeBollingerPattern(prices));

        // 6. 연속 상승/하락 후 반전 패턴
        patterns.Add(AnalyzeConsecutivePattern(weeklyData));

        // 7. 변동성 패턴
        patterns.Add(AnalyzeVolatilityPattern(prices));

        // 8. 거래량 패턴
        patterns.Add(AnalyzeVolumePattern(prices));

        return patterns.Where(p => p != null).ToList()!;
    }

    private PatternAnalysis AnalyzeDayOfWeekPattern(List<BitcoinPrice> prices)
    {
        // 요일별 상승/하락 확률 분석
        var dayStats = prices
            .GroupBy(p => p.Date.DayOfWeek)
            .ToDictionary(
                g => g.Key,
                g => new { Up = g.Count(p => p.IsUp), Total = g.Count() }
            );

        var today = DateTime.Now.DayOfWeek;
        var todayStats = dayStats.GetValueOrDefault(today);

        double upProb = 50;
        if (todayStats != null && todayStats.Total > 0)
        {
            upProb = (double)todayStats.Up / todayStats.Total * 100;
        }

        return new PatternAnalysis
        {
            PatternName = "요일별 패턴",
            Description = $"{today} 요일의 과거 상승 확률",
            UpProbability = upProb,
            DownProbability = 100 - upProb,
            OccurrenceCount = todayStats?.Total ?? 0,
            Confidence = Math.Min(100, (todayStats?.Total ?? 0) / 10.0 * 100),
            IsCurrentlyMatched = true
        };
    }

    private PatternAnalysis AnalyzeMonthPattern(List<BitcoinPrice> prices)
    {
        var monthStats = prices
            .GroupBy(p => p.Date.Month)
            .ToDictionary(
                g => g.Key,
                g => new { Up = g.Count(p => p.IsUp), Total = g.Count() }
            );

        var currentMonth = DateTime.Now.Month;
        var monthStat = monthStats.GetValueOrDefault(currentMonth);

        double upProb = 50;
        if (monthStat != null && monthStat.Total > 0)
        {
            upProb = (double)monthStat.Up / monthStat.Total * 100;
        }

        var monthNames = new[] { "", "1월", "2월", "3월", "4월", "5월", "6월",
                                  "7월", "8월", "9월", "10월", "11월", "12월" };

        return new PatternAnalysis
        {
            PatternName = "월별 패턴",
            Description = $"{monthNames[currentMonth]}의 과거 상승 확률",
            UpProbability = upProb,
            DownProbability = 100 - upProb,
            OccurrenceCount = monthStat?.Total ?? 0,
            Confidence = Math.Min(100, (monthStat?.Total ?? 0) / 50.0 * 100),
            IsCurrentlyMatched = true
        };
    }

    private PatternAnalysis AnalyzeMAPattern(List<BitcoinPrice> prices)
    {
        if (prices.Count < 100) return CreateEmptyPattern("이동평균선 패턴");

        var lastIndex = prices.Count - 1;
        var indicators = CalculateIndicators(prices, lastIndex);

        var currentPrice = prices[lastIndex].Close;
        var isGoldenCross = indicators.SMA7 > indicators.SMA25 && indicators.SMA25 > indicators.SMA99;
        var isDeathCross = indicators.SMA7 < indicators.SMA25 && indicators.SMA25 < indicators.SMA99;

        // 과거 골든크로스/데드크로스 후 상승 확률 계산
        int goldenCrossUp = 0, goldenCrossTotal = 0;
        int deathCrossDown = 0, deathCrossTotal = 0;

        for (int i = 100; i < prices.Count - 7; i++)
        {
            var ind = CalculateIndicators(prices, i);
            var prevInd = CalculateIndicators(prices, i - 1);

            // 골든크로스 감지
            if (ind.SMA7 > ind.SMA25 && prevInd.SMA7 <= prevInd.SMA25)
            {
                goldenCrossTotal++;
                // 일주일 후 가격 확인
                if (prices[i + 7].Close > prices[i].Close)
                    goldenCrossUp++;
            }

            // 데드크로스 감지
            if (ind.SMA7 < ind.SMA25 && prevInd.SMA7 >= prevInd.SMA25)
            {
                deathCrossTotal++;
                if (prices[i + 7].Close < prices[i].Close)
                    deathCrossDown++;
            }
        }

        double upProb = 50;
        int count = 0;
        string desc = "이동평균선 분석";

        if (isGoldenCross && goldenCrossTotal > 0)
        {
            upProb = (double)goldenCrossUp / goldenCrossTotal * 100;
            count = goldenCrossTotal;
            desc = "골든크로스 발생 (SMA7 > SMA25 > SMA99)";
        }
        else if (isDeathCross && deathCrossTotal > 0)
        {
            upProb = 100 - ((double)deathCrossDown / deathCrossTotal * 100);
            count = deathCrossTotal;
            desc = "데드크로스 발생 (SMA7 < SMA25 < SMA99)";
        }

        return new PatternAnalysis
        {
            PatternName = "이동평균선 패턴",
            Description = desc,
            UpProbability = upProb,
            DownProbability = 100 - upProb,
            OccurrenceCount = count,
            Confidence = Math.Min(100, count * 5.0),
            IsCurrentlyMatched = isGoldenCross || isDeathCross
        };
    }

    private PatternAnalysis AnalyzeRSIPattern(List<BitcoinPrice> prices)
    {
        if (prices.Count < 100) return CreateEmptyPattern("RSI 패턴");

        var lastIndex = prices.Count - 1;
        var indicators = CalculateIndicators(prices, lastIndex);

        var isOversold = indicators.RSI < 30;
        var isOverbought = indicators.RSI > 70;

        // 과매도/과매수 후 가격 변동 분석
        int oversoldUp = 0, oversoldTotal = 0;
        int overboughtDown = 0, overboughtTotal = 0;

        for (int i = 100; i < prices.Count - 7; i++)
        {
            var ind = CalculateIndicators(prices, i);

            if (ind.RSI < 30)
            {
                oversoldTotal++;
                if (prices[i + 7].Close > prices[i].Close)
                    oversoldUp++;
            }
            else if (ind.RSI > 70)
            {
                overboughtTotal++;
                if (prices[i + 7].Close < prices[i].Close)
                    overboughtDown++;
            }
        }

        double upProb = 50;
        int count = 0;
        string desc = $"RSI: {indicators.RSI:F1}";

        if (isOversold && oversoldTotal > 0)
        {
            upProb = (double)oversoldUp / oversoldTotal * 100;
            count = oversoldTotal;
            desc = $"과매도 구간 (RSI: {indicators.RSI:F1})";
        }
        else if (isOverbought && overboughtTotal > 0)
        {
            upProb = 100 - ((double)overboughtDown / overboughtTotal * 100);
            count = overboughtTotal;
            desc = $"과매수 구간 (RSI: {indicators.RSI:F1})";
        }

        return new PatternAnalysis
        {
            PatternName = "RSI 패턴",
            Description = desc,
            UpProbability = upProb,
            DownProbability = 100 - upProb,
            OccurrenceCount = count,
            Confidence = Math.Min(100, count * 3.0),
            IsCurrentlyMatched = isOversold || isOverbought
        };
    }

    private PatternAnalysis AnalyzeBollingerPattern(List<BitcoinPrice> prices)
    {
        if (prices.Count < 100) return CreateEmptyPattern("볼린저 밴드 패턴");

        var lastIndex = prices.Count - 1;
        var indicators = CalculateIndicators(prices, lastIndex);
        var currentPrice = prices[lastIndex].Close;

        var isNearLower = currentPrice <= indicators.BollingerLower * 1.02m;
        var isNearUpper = currentPrice >= indicators.BollingerUpper * 0.98m;

        int lowerUp = 0, lowerTotal = 0;
        int upperDown = 0, upperTotal = 0;

        for (int i = 100; i < prices.Count - 7; i++)
        {
            var ind = CalculateIndicators(prices, i);
            var price = prices[i].Close;

            if (price <= ind.BollingerLower * 1.02m)
            {
                lowerTotal++;
                if (prices[i + 7].Close > prices[i].Close)
                    lowerUp++;
            }
            else if (price >= ind.BollingerUpper * 0.98m)
            {
                upperTotal++;
                if (prices[i + 7].Close < prices[i].Close)
                    upperDown++;
            }
        }

        double upProb = 50;
        int count = 0;
        string desc = "볼린저 밴드 중간 구간";

        if (isNearLower && lowerTotal > 0)
        {
            upProb = (double)lowerUp / lowerTotal * 100;
            count = lowerTotal;
            desc = "볼린저 밴드 하단 근접";
        }
        else if (isNearUpper && upperTotal > 0)
        {
            upProb = 100 - ((double)upperDown / upperTotal * 100);
            count = upperTotal;
            desc = "볼린저 밴드 상단 근접";
        }

        return new PatternAnalysis
        {
            PatternName = "볼린저 밴드 패턴",
            Description = desc,
            UpProbability = upProb,
            DownProbability = 100 - upProb,
            OccurrenceCount = count,
            Confidence = Math.Min(100, count * 2.0),
            IsCurrentlyMatched = isNearLower || isNearUpper
        };
    }

    private PatternAnalysis AnalyzeConsecutivePattern(List<WeeklyData> weeklyData)
    {
        if (weeklyData.Count < 10) return CreateEmptyPattern("연속 패턴");

        // 현재까지 연속 상승/하락 횟수
        int consecutive = 0;
        bool isConsecutiveUp = false;

        for (int i = weeklyData.Count - 1; i >= 0; i--)
        {
            if (consecutive == 0)
            {
                isConsecutiveUp = weeklyData[i].IsUp;
                consecutive = 1;
            }
            else if (weeklyData[i].IsUp == isConsecutiveUp)
            {
                consecutive++;
            }
            else
            {
                break;
            }
        }

        // 과거 연속 패턴 후 반전 확률
        int reverseCount = 0, totalCount = 0;

        for (int i = consecutive; i < weeklyData.Count - 1; i++)
        {
            bool matchConsecutive = true;
            for (int j = 0; j < consecutive && i - j >= 0; j++)
            {
                if (weeklyData[i - j].IsUp != isConsecutiveUp)
                {
                    matchConsecutive = false;
                    break;
                }
            }

            if (matchConsecutive)
            {
                totalCount++;
                // 다음 주 반전 여부
                if (weeklyData[i + 1].IsUp != isConsecutiveUp)
                    reverseCount++;
            }
        }

        double reverseProb = totalCount > 0 ? (double)reverseCount / totalCount * 100 : 50;
        double upProb = isConsecutiveUp ? (100 - reverseProb) : reverseProb;

        string direction = isConsecutiveUp ? "상승" : "하락";

        return new PatternAnalysis
        {
            PatternName = "연속 패턴",
            Description = $"{consecutive}주 연속 {direction} 후",
            UpProbability = upProb,
            DownProbability = 100 - upProb,
            OccurrenceCount = totalCount,
            Confidence = Math.Min(100, totalCount * 10.0),
            IsCurrentlyMatched = consecutive >= 2
        };
    }

    private PatternAnalysis AnalyzeVolatilityPattern(List<BitcoinPrice> prices)
    {
        if (prices.Count < 30) return CreateEmptyPattern("변동성 패턴");

        // 최근 7일 변동성
        var recent7 = prices.TakeLast(7).ToList();
        var volatility7 = CalculateVolatility(recent7);

        // 최근 30일 평균 변동성
        var recent30 = prices.TakeLast(30).ToList();
        var volatility30 = CalculateVolatility(recent30);

        var isHighVolatility = volatility7 > volatility30 * 1.5;
        var isLowVolatility = volatility7 < volatility30 * 0.5;

        // 변동성 패턴 후 가격 변동 분석
        int highVolUp = 0, highVolTotal = 0;
        int lowVolUp = 0, lowVolTotal = 0;

        for (int i = 30; i < prices.Count - 7; i++)
        {
            var vol7 = CalculateVolatility(prices.Skip(i - 7).Take(7).ToList());
            var vol30 = CalculateVolatility(prices.Skip(i - 30).Take(30).ToList());

            if (vol7 > vol30 * 1.5)
            {
                highVolTotal++;
                if (prices[i + 7].Close > prices[i].Close) highVolUp++;
            }
            else if (vol7 < vol30 * 0.5)
            {
                lowVolTotal++;
                if (prices[i + 7].Close > prices[i].Close) lowVolUp++;
            }
        }

        double upProb = 50;
        int count = 0;
        string desc = "정상 변동성";

        if (isHighVolatility && highVolTotal > 0)
        {
            upProb = (double)highVolUp / highVolTotal * 100;
            count = highVolTotal;
            desc = "높은 변동성 구간";
        }
        else if (isLowVolatility && lowVolTotal > 0)
        {
            upProb = (double)lowVolUp / lowVolTotal * 100;
            count = lowVolTotal;
            desc = "낮은 변동성 구간 (폭발 가능성)";
        }

        return new PatternAnalysis
        {
            PatternName = "변동성 패턴",
            Description = desc,
            UpProbability = upProb,
            DownProbability = 100 - upProb,
            OccurrenceCount = count,
            Confidence = Math.Min(100, count * 2.0),
            IsCurrentlyMatched = isHighVolatility || isLowVolatility
        };
    }

    private decimal CalculateVolatility(List<BitcoinPrice> prices)
    {
        if (prices.Count < 2) return 0;

        var returns = new List<decimal>();
        for (int i = 1; i < prices.Count; i++)
        {
            if (prices[i - 1].Close != 0)
            {
                returns.Add((prices[i].Close - prices[i - 1].Close) / prices[i - 1].Close);
            }
        }

        if (returns.Count == 0) return 0;

        var avg = returns.Average();
        var sumSquares = returns.Sum(r => (r - avg) * (r - avg));
        return (decimal)Math.Sqrt((double)(sumSquares / returns.Count));
    }

    private PatternAnalysis AnalyzeVolumePattern(List<BitcoinPrice> prices)
    {
        if (prices.Count < 30) return CreateEmptyPattern("거래량 패턴");

        var recent7Volume = prices.TakeLast(7).Average(p => p.Volume);
        var recent30Volume = prices.TakeLast(30).Average(p => p.Volume);

        var isHighVolume = recent7Volume > recent30Volume * 1.5m;
        var isLowVolume = recent7Volume < recent30Volume * 0.5m;

        double upProb = 50;
        string desc = "정상 거래량";

        if (isHighVolume)
        {
            desc = "거래량 급증 (추세 강화 가능)";
            // 거래량 급증 시 현재 추세 방향 유지 확률 높음
            var isCurrentUp = prices.Last().IsUp;
            upProb = isCurrentUp ? 60 : 40;
        }
        else if (isLowVolume)
        {
            desc = "거래량 감소 (추세 전환 가능)";
            upProb = 50;
        }

        return new PatternAnalysis
        {
            PatternName = "거래량 패턴",
            Description = desc,
            UpProbability = upProb,
            DownProbability = 100 - upProb,
            OccurrenceCount = 0,
            Confidence = 30,
            IsCurrentlyMatched = isHighVolume || isLowVolume
        };
    }

    private PatternAnalysis CreateEmptyPattern(string name)
    {
        return new PatternAnalysis
        {
            PatternName = name,
            Description = "데이터 부족",
            UpProbability = 50,
            DownProbability = 50,
            OccurrenceCount = 0,
            Confidence = 0,
            IsCurrentlyMatched = false
        };
    }
}
