<?php
/**
 * 비트코인 가격 예측 엔진
 */

class PredictionEngine {

    /**
     * 기술적 지표 계산
     */
    public function calculateIndicators($prices, $currentIndex) {
        $indicators = [
            'sma7' => 0,
            'sma25' => 0,
            'sma99' => 0,
            'rsi' => 50,
            'macd' => 0,
            'bollinger_upper' => 0,
            'bollinger_middle' => 0,
            'bollinger_lower' => 0
        ];

        if ($currentIndex < 99 || count($prices) <= $currentIndex) {
            return $indicators;
        }

        $indicators['sma7'] = $this->calculateSMA($prices, $currentIndex, 7);
        $indicators['sma25'] = $this->calculateSMA($prices, $currentIndex, 25);
        $indicators['sma99'] = $this->calculateSMA($prices, $currentIndex, 99);
        $indicators['rsi'] = $this->calculateRSI($prices, $currentIndex, 14);

        list($upper, $middle, $lower) = $this->calculateBollingerBands($prices, $currentIndex, 20, 2);
        $indicators['bollinger_upper'] = $upper;
        $indicators['bollinger_middle'] = $middle;
        $indicators['bollinger_lower'] = $lower;

        return $indicators;
    }

    private function calculateSMA($prices, $endIndex, $period) {
        if ($endIndex < $period - 1) return 0;

        $sum = 0;
        for ($i = $endIndex - $period + 1; $i <= $endIndex; $i++) {
            $sum += $prices[$i]['close'];
        }
        return $sum / $period;
    }

    private function calculateRSI($prices, $endIndex, $period) {
        if ($endIndex < $period) return 50;

        $gains = 0;
        $losses = 0;

        for ($i = $endIndex - $period + 1; $i <= $endIndex; $i++) {
            $change = $prices[$i]['close'] - $prices[$i - 1]['close'];
            if ($change > 0) $gains += $change;
            else $losses += abs($change);
        }

        $avgGain = $gains / $period;
        $avgLoss = $losses / $period;

        if ($avgLoss == 0) return 100;

        $rs = $avgGain / $avgLoss;
        return 100 - (100 / (1 + $rs));
    }

    private function calculateBollingerBands($prices, $endIndex, $period, $stdDevMultiplier) {
        if ($endIndex < $period - 1) return [0, 0, 0];

        $middle = $this->calculateSMA($prices, $endIndex, $period);

        $sumSquares = 0;
        for ($i = $endIndex - $period + 1; $i <= $endIndex; $i++) {
            $diff = $prices[$i]['close'] - $middle;
            $sumSquares += $diff * $diff;
        }

        $stdDev = sqrt($sumSquares / $period);
        $upper = $middle + ($stdDevMultiplier * $stdDev);
        $lower = $middle - ($stdDevMultiplier * $stdDev);

        return [$upper, $middle, $lower];
    }

    /**
     * 모든 패턴 분석
     */
    public function analyzePatterns($prices, $weeklyData) {
        $patterns = [];

        $patterns[] = $this->analyzeDayOfWeekPattern($prices);
        $patterns[] = $this->analyzeMonthPattern($prices);
        $patterns[] = $this->analyzeMAPattern($prices);
        $patterns[] = $this->analyzeRSIPattern($prices);
        $patterns[] = $this->analyzeBollingerPattern($prices);
        $patterns[] = $this->analyzeConsecutivePattern($weeklyData);

        return array_filter($patterns);
    }

    private function analyzeDayOfWeekPattern($prices) {
        $dayStats = [];

        foreach ($prices as $price) {
            $dayOfWeek = date('N', strtotime($price['date']));
            if (!isset($dayStats[$dayOfWeek])) {
                $dayStats[$dayOfWeek] = ['up' => 0, 'total' => 0];
            }
            $dayStats[$dayOfWeek]['total']++;
            if ($price['close'] > $price['open']) {
                $dayStats[$dayOfWeek]['up']++;
            }
        }

        $today = date('N');
        $todayStats = $dayStats[$today] ?? ['up' => 0, 'total' => 0];

        $upProb = 50;
        if ($todayStats['total'] > 0) {
            $upProb = ($todayStats['up'] / $todayStats['total']) * 100;
        }

        $dayNames = ['', '월', '화', '수', '목', '금', '토', '일'];

        return [
            'name' => '요일별 패턴',
            'description' => "{$dayNames[$today]}요일의 과거 상승 확률",
            'up_probability' => round($upProb, 1),
            'down_probability' => round(100 - $upProb, 1),
            'occurrence_count' => $todayStats['total'],
            'confidence' => min(100, ($todayStats['total'] / 10) * 100),
            'is_matched' => true
        ];
    }

    private function analyzeMonthPattern($prices) {
        $monthStats = [];

        foreach ($prices as $price) {
            $month = date('n', strtotime($price['date']));
            if (!isset($monthStats[$month])) {
                $monthStats[$month] = ['up' => 0, 'total' => 0];
            }
            $monthStats[$month]['total']++;
            if ($price['close'] > $price['open']) {
                $monthStats[$month]['up']++;
            }
        }

        $currentMonth = date('n');
        $monthStat = $monthStats[$currentMonth] ?? ['up' => 0, 'total' => 0];

        $upProb = 50;
        if ($monthStat['total'] > 0) {
            $upProb = ($monthStat['up'] / $monthStat['total']) * 100;
        }

        return [
            'name' => '월별 패턴',
            'description' => "{$currentMonth}월의 과거 상승 확률",
            'up_probability' => round($upProb, 1),
            'down_probability' => round(100 - $upProb, 1),
            'occurrence_count' => $monthStat['total'],
            'confidence' => min(100, ($monthStat['total'] / 50) * 100),
            'is_matched' => true
        ];
    }

    private function analyzeMAPattern($prices) {
        if (count($prices) < 100) {
            return $this->createEmptyPattern('이동평균선 패턴');
        }

        $lastIndex = count($prices) - 1;
        $indicators = $this->calculateIndicators($prices, $lastIndex);

        $isGoldenCross = $indicators['sma7'] > $indicators['sma25'] &&
                         $indicators['sma25'] > $indicators['sma99'];
        $isDeathCross = $indicators['sma7'] < $indicators['sma25'] &&
                        $indicators['sma25'] < $indicators['sma99'];

        $desc = '이동평균선 분석';
        $upProb = 50;

        if ($isGoldenCross) {
            $desc = '골든크로스 (SMA7 > SMA25 > SMA99)';
            $upProb = 65;
        } else if ($isDeathCross) {
            $desc = '데드크로스 (SMA7 < SMA25 < SMA99)';
            $upProb = 35;
        }

        return [
            'name' => '이동평균선 패턴',
            'description' => $desc,
            'up_probability' => $upProb,
            'down_probability' => 100 - $upProb,
            'occurrence_count' => 0,
            'confidence' => ($isGoldenCross || $isDeathCross) ? 60 : 30,
            'is_matched' => $isGoldenCross || $isDeathCross
        ];
    }

    private function analyzeRSIPattern($prices) {
        if (count($prices) < 100) {
            return $this->createEmptyPattern('RSI 패턴');
        }

        $lastIndex = count($prices) - 1;
        $indicators = $this->calculateIndicators($prices, $lastIndex);
        $rsi = $indicators['rsi'];

        $isOversold = $rsi < 30;
        $isOverbought = $rsi > 70;

        $desc = sprintf('RSI: %.1f', $rsi);
        $upProb = 50;

        if ($isOversold) {
            $desc = sprintf('과매도 구간 (RSI: %.1f)', $rsi);
            $upProb = 65;
        } else if ($isOverbought) {
            $desc = sprintf('과매수 구간 (RSI: %.1f)', $rsi);
            $upProb = 35;
        }

        return [
            'name' => 'RSI 패턴',
            'description' => $desc,
            'up_probability' => $upProb,
            'down_probability' => 100 - $upProb,
            'occurrence_count' => 0,
            'confidence' => ($isOversold || $isOverbought) ? 55 : 30,
            'is_matched' => $isOversold || $isOverbought
        ];
    }

    private function analyzeBollingerPattern($prices) {
        if (count($prices) < 100) {
            return $this->createEmptyPattern('볼린저 밴드 패턴');
        }

        $lastIndex = count($prices) - 1;
        $indicators = $this->calculateIndicators($prices, $lastIndex);
        $currentPrice = $prices[$lastIndex]['close'];

        $isNearLower = $currentPrice <= $indicators['bollinger_lower'] * 1.02;
        $isNearUpper = $currentPrice >= $indicators['bollinger_upper'] * 0.98;

        $desc = '볼린저 밴드 중간 구간';
        $upProb = 50;

        if ($isNearLower) {
            $desc = '볼린저 밴드 하단 근접';
            $upProb = 60;
        } else if ($isNearUpper) {
            $desc = '볼린저 밴드 상단 근접';
            $upProb = 40;
        }

        return [
            'name' => '볼린저 밴드 패턴',
            'description' => $desc,
            'up_probability' => $upProb,
            'down_probability' => 100 - $upProb,
            'occurrence_count' => 0,
            'confidence' => ($isNearLower || $isNearUpper) ? 50 : 25,
            'is_matched' => $isNearLower || $isNearUpper
        ];
    }

    private function analyzeConsecutivePattern($weeklyData) {
        if (count($weeklyData) < 10) {
            return $this->createEmptyPattern('연속 패턴');
        }

        $consecutive = 0;
        $isConsecutiveUp = false;

        for ($i = count($weeklyData) - 1; $i >= 0; $i--) {
            if ($consecutive == 0) {
                $isConsecutiveUp = $weeklyData[$i]['is_up'];
                $consecutive = 1;
            } else if ($weeklyData[$i]['is_up'] == $isConsecutiveUp) {
                $consecutive++;
            } else {
                break;
            }
        }

        $direction = $isConsecutiveUp ? '상승' : '하락';

        // 연속 후 반전 확률 (경험적)
        $reverseProb = min(70, 40 + ($consecutive * 5));
        $upProb = $isConsecutiveUp ? (100 - $reverseProb) : $reverseProb;

        return [
            'name' => '연속 패턴',
            'description' => "{$consecutive}주 연속 {$direction} 후",
            'up_probability' => round($upProb, 1),
            'down_probability' => round(100 - $upProb, 1),
            'occurrence_count' => 0,
            'confidence' => min(100, $consecutive * 15),
            'is_matched' => $consecutive >= 2
        ];
    }

    private function createEmptyPattern($name) {
        return [
            'name' => $name,
            'description' => '데이터 부족',
            'up_probability' => 50,
            'down_probability' => 50,
            'occurrence_count' => 0,
            'confidence' => 0,
            'is_matched' => false
        ];
    }

    /**
     * 예측 생성
     */
    public function generatePrediction($prices, $weeklyData) {
        $prediction = [
            'prediction_date' => date('Y-m-d H:i:s'),
            'target_week_start' => $this->getWeekStart(),
            'target_week_end' => $this->getWeekEnd(),
            'direction' => 'Unknown',
            'probability' => 50,
            'confidence' => 0,
            'patterns' => [],
            'indicators' => [],
            'summary' => '',
            'reasons' => []
        ];

        if (count($prices) < 100) {
            $prediction['summary'] = '데이터가 부족하여 예측이 불가능합니다.';
            return $prediction;
        }

        // 지표 계산
        $lastIndex = count($prices) - 1;
        $prediction['indicators'] = $this->calculateIndicators($prices, $lastIndex);
        $prediction['current_price'] = $prices[$lastIndex]['close'];

        // 패턴 분석
        $prediction['patterns'] = $this->analyzePatterns($prices, $weeklyData);

        // 가중 점수 계산
        $weightedScore = $this->calculateWeightedScore($prediction['patterns']);

        // 예측 방향 결정
        if ($weightedScore > 55) {
            $prediction['direction'] = 'Up';
            $prediction['probability'] = $weightedScore;
        } else if ($weightedScore < 45) {
            $prediction['direction'] = 'Down';
            $prediction['probability'] = 100 - $weightedScore;
        } else {
            $prediction['direction'] = 'Neutral';
            $prediction['probability'] = 50;
        }

        // 신뢰도 계산
        $prediction['confidence'] = $this->calculateConfidence($prediction['patterns']);

        // 요약 생성
        $this->generateSummary($prediction);

        return $prediction;
    }

    private function calculateWeightedScore($patterns) {
        $weights = [
            '이동평균선 패턴' => 2.0,
            'RSI 패턴' => 1.8,
            '볼린저 밴드 패턴' => 1.5,
            '연속 패턴' => 1.5,
            '요일별 패턴' => 0.5,
            '월별 패턴' => 0.8
        ];

        $totalWeight = 0;
        $weightedSum = 0;

        foreach ($patterns as $pattern) {
            $weight = $weights[$pattern['name']] ?? 1.0;

            if ($pattern['is_matched']) {
                $weight *= 1.5;
            }

            $weight *= $pattern['confidence'] / 100;

            $totalWeight += $weight;
            $weightedSum += $pattern['up_probability'] * $weight;
        }

        return $totalWeight > 0 ? $weightedSum / $totalWeight : 50;
    }

    private function calculateConfidence($patterns) {
        $matchedPatterns = array_filter($patterns, function($p) {
            return $p['is_matched'];
        });

        if (count($matchedPatterns) == 0) {
            return 20;
        }

        $avgConfidence = array_sum(array_column($matchedPatterns, 'confidence')) / count($matchedPatterns);

        $upCount = count(array_filter($matchedPatterns, function($p) {
            return $p['up_probability'] > 55;
        }));

        $downCount = count(array_filter($matchedPatterns, function($p) {
            return $p['up_probability'] < 45;
        }));

        $agreement = max($upCount, $downCount) / count($matchedPatterns);

        return min(100, $avgConfidence * $agreement * 1.2);
    }

    private function generateSummary(&$prediction) {
        $directionText = [
            'Up' => '상승',
            'Down' => '하락',
            'Neutral' => '횡보'
        ];

        $direction = $directionText[$prediction['direction']] ?? '알 수 없음';

        $confidenceText = '낮음';
        if ($prediction['confidence'] >= 70) $confidenceText = '높음';
        else if ($prediction['confidence'] >= 50) $confidenceText = '중간';

        $prediction['summary'] = sprintf(
            "이번 주 비트코인 예측: %s (확률 %.1f%%, 신뢰도 %s)",
            $direction,
            $prediction['probability'],
            $confidenceText
        );

        // 이유 생성
        $prediction['reasons'] = [];

        $matchedPatterns = array_filter($prediction['patterns'], function($p) {
            return $p['is_matched'];
        });

        usort($matchedPatterns, function($a, $b) {
            return $b['confidence'] - $a['confidence'];
        });

        foreach (array_slice($matchedPatterns, 0, 5) as $pattern) {
            $patternDir = $pattern['up_probability'] > 55 ? '상승' :
                         ($pattern['up_probability'] < 45 ? '하락' : '중립');
            $prediction['reasons'][] = sprintf(
                "[%s] %s → %s 신호 (%.1f%%)",
                $pattern['name'],
                $pattern['description'],
                $patternDir,
                $pattern['up_probability']
            );
        }

        // 기술적 지표 요약
        $ind = $prediction['indicators'];
        $prediction['reasons'][] = '';
        $prediction['reasons'][] = '📊 현재 기술적 지표:';
        $prediction['reasons'][] = sprintf('  • RSI: %.1f %s',
            $ind['rsi'],
            $this->getRSIStatus($ind['rsi'])
        );
        $prediction['reasons'][] = sprintf('  • 현재가격 vs SMA25: %s',
            $prediction['current_price'] > $ind['sma25'] ? '위' : '아래'
        );
    }

    private function getRSIStatus($rsi) {
        if ($rsi > 70) return '(과매수 - 조정 가능성)';
        if ($rsi < 30) return '(과매도 - 반등 가능성)';
        if ($rsi > 50) return '(상승 추세)';
        return '(하락 추세)';
    }

    private function getWeekStart() {
        $today = new DateTime();
        $dayOfWeek = $today->format('N');
        $today->modify('-' . ($dayOfWeek - 1) . ' days');
        return $today->format('Y-m-d');
    }

    private function getWeekEnd() {
        $today = new DateTime();
        $dayOfWeek = $today->format('N');
        $today->modify('+' . (7 - $dayOfWeek) . ' days');
        return $today->format('Y-m-d');
    }
}
