namespace BitcoinPredictor.Models;

/// <summary>
/// 비트코인 가격 데이터 모델
/// </summary>
public class BitcoinPrice
{
    public DateTime Date { get; set; }
    public decimal Open { get; set; }
    public decimal High { get; set; }
    public decimal Low { get; set; }
    public decimal Close { get; set; }
    public decimal Volume { get; set; }

    /// <summary>
    /// 일일 변동률 (%)
    /// </summary>
    public decimal DailyChangePercent => Open != 0 ? ((Close - Open) / Open) * 100 : 0;

    /// <summary>
    /// 상승 여부
    /// </summary>
    public bool IsUp => Close > Open;
}

/// <summary>
/// 주간 데이터 요약
/// </summary>
public class WeeklyData
{
    public int Year { get; set; }
    public int WeekNumber { get; set; }
    public DateTime WeekStart { get; set; }
    public DateTime WeekEnd { get; set; }
    public decimal OpenPrice { get; set; }
    public decimal ClosePrice { get; set; }
    public decimal HighPrice { get; set; }
    public decimal LowPrice { get; set; }
    public decimal WeeklyChangePercent { get; set; }
    public bool IsUp { get; set; }
    public List<BitcoinPrice> DailyPrices { get; set; } = new();
}

/// <summary>
/// 기술적 지표
/// </summary>
public class TechnicalIndicators
{
    public decimal SMA7 { get; set; }      // 7일 단순이동평균
    public decimal SMA25 { get; set; }     // 25일 단순이동평균
    public decimal SMA99 { get; set; }     // 99일 단순이동평균
    public decimal RSI { get; set; }       // 상대강도지수 (14일)
    public decimal MACD { get; set; }      // MACD
    public decimal MACDSignal { get; set; }// MACD 시그널
    public decimal BollingerUpper { get; set; }  // 볼린저 밴드 상단
    public decimal BollingerMiddle { get; set; } // 볼린저 밴드 중간
    public decimal BollingerLower { get; set; }  // 볼린저 밴드 하단
}

/// <summary>
/// 패턴 분석 결과
/// </summary>
public class PatternAnalysis
{
    public string PatternName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public double UpProbability { get; set; }      // 상승 확률 (0-100)
    public double DownProbability { get; set; }    // 하락 확률 (0-100)
    public int OccurrenceCount { get; set; }       // 발생 횟수
    public double Confidence { get; set; }         // 신뢰도 (0-100)
    public bool IsCurrentlyMatched { get; set; }   // 현재 패턴 일치 여부
}

/// <summary>
/// 예측 결과
/// </summary>
public class PredictionResult
{
    public DateTime PredictionDate { get; set; }
    public DateTime TargetWeekStart { get; set; }
    public DateTime TargetWeekEnd { get; set; }

    public string Direction { get; set; } = "Unknown"; // "Up", "Down", "Neutral"
    public double Probability { get; set; }            // 확률 (0-100)
    public double Confidence { get; set; }             // 신뢰도 (0-100)

    public List<PatternAnalysis> MatchedPatterns { get; set; } = new();
    public TechnicalIndicators CurrentIndicators { get; set; } = new();

    public string Summary { get; set; } = string.Empty;
    public List<string> Reasons { get; set; } = new();
}
