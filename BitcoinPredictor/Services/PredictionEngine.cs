using BitcoinPredictor.Models;

namespace BitcoinPredictor.Services;

/// <summary>
/// 비트코인 가격 예측 엔진
/// 다양한 패턴과 지표를 종합하여 예측
/// </summary>
public class PredictionEngine
{
    private readonly PatternAnalysisService _patternService;

    public PredictionEngine()
    {
        _patternService = new PatternAnalysisService();
    }

    /// <summary>
    /// 이번 주 가격 예측 생성
    /// </summary>
    public PredictionResult GeneratePrediction(List<BitcoinPrice> prices, List<WeeklyData> weeklyData)
    {
        var prediction = new PredictionResult
        {
            PredictionDate = DateTime.Now,
            TargetWeekStart = GetCurrentWeekStart(),
            TargetWeekEnd = GetCurrentWeekEnd()
        };

        if (prices.Count < 100)
        {
            prediction.Direction = "Unknown";
            prediction.Summary = "데이터가 부족하여 예측이 불가능합니다.";
            return prediction;
        }

        // 1. 기술적 지표 계산
        prediction.CurrentIndicators = _patternService.CalculateIndicators(prices, prices.Count - 1);

        // 2. 패턴 분석
        prediction.MatchedPatterns = _patternService.AnalyzeAllPatterns(prices, weeklyData);

        // 3. 종합 점수 계산 (가중 평균)
        var weightedScore = CalculateWeightedScore(prediction.MatchedPatterns);

        // 4. 예측 방향 결정
        if (weightedScore > 55)
        {
            prediction.Direction = "Up";
            prediction.Probability = weightedScore;
        }
        else if (weightedScore < 45)
        {
            prediction.Direction = "Down";
            prediction.Probability = 100 - weightedScore;
        }
        else
        {
            prediction.Direction = "Neutral";
            prediction.Probability = 50;
        }

        // 5. 신뢰도 계산
        prediction.Confidence = CalculateConfidence(prediction.MatchedPatterns);

        // 6. 요약 및 이유 생성
        GenerateSummary(prediction, prices.Last().Close);

        return prediction;
    }

    private double CalculateWeightedScore(List<PatternAnalysis> patterns)
    {
        var totalWeight = 0.0;
        var weightedSum = 0.0;

        // 패턴별 가중치
        var weights = new Dictionary<string, double>
        {
            { "이동평균선 패턴", 2.0 },
            { "RSI 패턴", 1.8 },
            { "볼린저 밴드 패턴", 1.5 },
            { "연속 패턴", 1.5 },
            { "변동성 패턴", 1.2 },
            { "거래량 패턴", 1.0 },
            { "요일별 패턴", 0.5 },
            { "월별 패턴", 0.8 }
        };

        foreach (var pattern in patterns)
        {
            var weight = weights.GetValueOrDefault(pattern.PatternName, 1.0);

            // 현재 일치하는 패턴에 더 높은 가중치
            if (pattern.IsCurrentlyMatched)
                weight *= 1.5;

            // 신뢰도에 따른 가중치 조정
            weight *= pattern.Confidence / 100.0;

            totalWeight += weight;
            weightedSum += pattern.UpProbability * weight;
        }

        return totalWeight > 0 ? weightedSum / totalWeight : 50;
    }

    private double CalculateConfidence(List<PatternAnalysis> patterns)
    {
        // 일치하는 패턴들의 평균 신뢰도
        var matchedPatterns = patterns.Where(p => p.IsCurrentlyMatched).ToList();

        if (matchedPatterns.Count == 0)
            return 20; // 일치하는 패턴 없으면 낮은 신뢰도

        var avgConfidence = matchedPatterns.Average(p => p.Confidence);

        // 방향 일치도 확인 (패턴들이 같은 방향을 가리키면 신뢰도 상승)
        var upCount = matchedPatterns.Count(p => p.UpProbability > 55);
        var downCount = matchedPatterns.Count(p => p.UpProbability < 45);
        var agreement = Math.Max(upCount, downCount) / (double)matchedPatterns.Count;

        return Math.Min(100, avgConfidence * agreement * 1.2);
    }

    private void GenerateSummary(PredictionResult prediction, decimal currentPrice)
    {
        var direction = prediction.Direction switch
        {
            "Up" => "상승",
            "Down" => "하락",
            _ => "횡보"
        };

        var confidence = prediction.Confidence switch
        {
            >= 70 => "높음",
            >= 50 => "중간",
            _ => "낮음"
        };

        prediction.Summary = $"이번 주 비트코인 예측: {direction} (확률 {prediction.Probability:F1}%, 신뢰도 {confidence})";

        // 이유 생성
        prediction.Reasons = new List<string>();

        var matchedPatterns = prediction.MatchedPatterns
            .Where(p => p.IsCurrentlyMatched)
            .OrderByDescending(p => p.Confidence)
            .Take(5);

        foreach (var pattern in matchedPatterns)
        {
            var patternDirection = pattern.UpProbability > 55 ? "상승" : pattern.UpProbability < 45 ? "하락" : "중립";
            prediction.Reasons.Add($"[{pattern.PatternName}] {pattern.Description} → {patternDirection} 신호 ({pattern.UpProbability:F1}%)");
        }

        // 기술적 지표 요약
        var ind = prediction.CurrentIndicators;
        prediction.Reasons.Add($"");
        prediction.Reasons.Add($"📊 현재 기술적 지표:");
        prediction.Reasons.Add($"  • RSI: {ind.RSI:F1} {GetRSIStatus(ind.RSI)}");
        prediction.Reasons.Add($"  • 현재가격 vs SMA25: {(currentPrice > ind.SMA25 ? "위" : "아래")}");
        prediction.Reasons.Add($"  • 볼린저밴드: {GetBollingerStatus(currentPrice, ind)}");
    }

    private string GetRSIStatus(decimal rsi)
    {
        if (rsi > 70) return "(과매수 - 조정 가능성)";
        if (rsi < 30) return "(과매도 - 반등 가능성)";
        if (rsi > 50) return "(상승 추세)";
        return "(하락 추세)";
    }

    private string GetBollingerStatus(decimal price, TechnicalIndicators ind)
    {
        if (price >= ind.BollingerUpper * 0.98m) return "상단 근접 (과열)";
        if (price <= ind.BollingerLower * 1.02m) return "하단 근접 (매수 기회?)";
        if (price > ind.BollingerMiddle) return "중간선 위";
        return "중간선 아래";
    }

    private DateTime GetCurrentWeekStart()
    {
        var today = DateTime.Today;
        var diff = (7 + (today.DayOfWeek - DayOfWeek.Monday)) % 7;
        return today.AddDays(-diff);
    }

    private DateTime GetCurrentWeekEnd()
    {
        return GetCurrentWeekStart().AddDays(6);
    }

    /// <summary>
    /// 백테스트 - 과거 예측 정확도 검증
    /// </summary>
    public BacktestResult RunBacktest(List<BitcoinPrice> prices, List<WeeklyData> weeklyData, int weeks = 52)
    {
        var result = new BacktestResult();

        if (weeklyData.Count < weeks + 10)
        {
            result.Summary = "백테스트를 위한 데이터가 부족합니다.";
            return result;
        }

        // 최근 N주에 대해 백테스트
        for (int i = weeklyData.Count - weeks - 1; i < weeklyData.Count - 1; i++)
        {
            // i번째 주까지의 데이터로 예측
            var testPrices = prices.Where(p => p.Date < weeklyData[i].WeekEnd).ToList();
            var testWeekly = weeklyData.Take(i + 1).ToList();

            if (testPrices.Count < 100) continue;

            var prediction = GeneratePrediction(testPrices, testWeekly);

            // 다음 주 실제 결과
            var actualUp = weeklyData[i + 1].IsUp;
            var predictedUp = prediction.Direction == "Up";

            result.TotalPredictions++;

            if (prediction.Direction == "Neutral")
            {
                result.NeutralPredictions++;
            }
            else if ((predictedUp && actualUp) || (!predictedUp && !actualUp))
            {
                result.CorrectPredictions++;
            }
            else
            {
                result.WrongPredictions++;
            }
        }

        var accuracy = result.TotalPredictions > 0
            ? (double)result.CorrectPredictions / (result.TotalPredictions - result.NeutralPredictions) * 100
            : 0;

        result.Accuracy = accuracy;
        result.Summary = $"백테스트 결과: {result.TotalPredictions}주 중 {result.CorrectPredictions}주 적중 " +
                        $"(정확도: {accuracy:F1}%, 중립: {result.NeutralPredictions}회)";

        return result;
    }
}

public class BacktestResult
{
    public int TotalPredictions { get; set; }
    public int CorrectPredictions { get; set; }
    public int WrongPredictions { get; set; }
    public int NeutralPredictions { get; set; }
    public double Accuracy { get; set; }
    public string Summary { get; set; } = string.Empty;
}
