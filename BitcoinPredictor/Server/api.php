<?php
/**
 * 비트코인 예측 API
 * WPF 앱에서 호출하거나 외부에서 사용
 */

header('Content-Type: application/json; charset=utf-8');
header('Access-Control-Allow-Origin: *');

require_once __DIR__ . '/config.php';
require_once __DIR__ . '/includes/BitcoinData.php';
require_once __DIR__ . '/includes/PredictionEngine.php';

$action = $_GET['action'] ?? 'prediction';

try {
    $bitcoinData = new BitcoinData();
    $predictionEngine = new PredictionEngine();

    switch ($action) {
        case 'price':
            // 현재 가격만
            $response = [
                'success' => true,
                'data' => $bitcoinData->getCurrentPrice(),
                'timestamp' => date('c')
            ];
            break;

        case 'prediction':
            // 예측 결과
            $prices = $bitcoinData->getHistoricalData();
            $weeklyData = $bitcoinData->convertToWeeklyData($prices);
            $prediction = $predictionEngine->generatePrediction($prices, $weeklyData);
            $prediction['current_price'] = $bitcoinData->getCurrentPrice()['usd'];

            $response = [
                'success' => true,
                'data' => $prediction,
                'timestamp' => date('c')
            ];
            break;

        case 'indicators':
            // 기술적 지표만
            $prices = $bitcoinData->getHistoricalData();
            $indicators = $predictionEngine->calculateIndicators($prices, count($prices) - 1);
            $indicators['current_price'] = $bitcoinData->getCurrentPrice()['usd'];

            $response = [
                'success' => true,
                'data' => $indicators,
                'timestamp' => date('c')
            ];
            break;

        case 'history':
            // 최근 N일 가격 히스토리
            $days = min(365, max(7, intval($_GET['days'] ?? 90)));
            $prices = $bitcoinData->getHistoricalData();
            $recentPrices = array_slice($prices, -$days);

            $response = [
                'success' => true,
                'data' => $recentPrices,
                'count' => count($recentPrices),
                'timestamp' => date('c')
            ];
            break;

        default:
            $response = [
                'success' => false,
                'error' => 'Unknown action',
                'available_actions' => ['price', 'prediction', 'indicators', 'history']
            ];
    }

} catch (Exception $e) {
    $response = [
        'success' => false,
        'error' => $e->getMessage()
    ];
}

echo json_encode($response, JSON_PRETTY_PRINT | JSON_UNESCAPED_UNICODE);
