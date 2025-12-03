<?php
/**
 * 주간 예측 크론잡 스크립트
 *
 * 닷홈 크론탭 설정:
 * 매주 월요일 오전 8시에 실행
 * 0 8 * * 1 php /home/사이트명/www/bitcoin/cron_weekly.php
 */

// CLI에서만 실행 가능
if (php_sapi_name() !== 'cli' && !isset($_GET['key'])) {
    // 웹에서 접근시 보안 키 필요
    die('Access denied');
}

// 보안 키 확인 (웹에서 테스트용)
$securityKey = 'your-secret-key-here'; // 변경 필요!
if (isset($_GET['key']) && $_GET['key'] !== $securityKey) {
    die('Invalid key');
}

require_once __DIR__ . '/config.php';
require_once __DIR__ . '/includes/BitcoinData.php';
require_once __DIR__ . '/includes/PredictionEngine.php';
require_once __DIR__ . '/includes/NotificationService.php';

echo "=== 비트코인 주간 예측 크론잡 시작 ===\n";
echo "시간: " . date('Y-m-d H:i:s') . "\n\n";

try {
    // 데이터 수집
    echo "1. 비트코인 데이터 수집 중...\n";
    $bitcoinData = new BitcoinData();
    $prices = $bitcoinData->getHistoricalData();
    echo "   - {$prices[count($prices)-1]['date']}까지 " . count($prices) . "일 데이터 수집 완료\n";

    // 주간 데이터 변환
    echo "2. 주간 데이터 변환 중...\n";
    $weeklyData = $bitcoinData->convertToWeeklyData($prices);
    echo "   - " . count($weeklyData) . "주 데이터 변환 완료\n";

    // 현재 가격
    $currentPrice = $bitcoinData->getCurrentPrice();
    echo "3. 현재 가격: \${$currentPrice['usd']} USD\n";

    // 예측 생성
    echo "4. 예측 생성 중...\n";
    $predictionEngine = new PredictionEngine();
    $prediction = $predictionEngine->generatePrediction($prices, $weeklyData);
    $prediction['current_price'] = $currentPrice['usd'];

    echo "   - 예측 방향: {$prediction['direction']}\n";
    echo "   - 확률: {$prediction['probability']}%\n";
    echo "   - 신뢰도: {$prediction['confidence']}%\n";

    // 예측 결과 저장
    $predictionFile = CACHE_DIR . 'prediction_' . date('Y-m-d') . '.json';
    file_put_contents($predictionFile, json_encode($prediction, JSON_PRETTY_PRINT | JSON_UNESCAPED_UNICODE));
    echo "5. 예측 결과 저장: {$predictionFile}\n";

    // 이메일 발송
    echo "6. 이메일 발송 중...\n";
    $notification = new NotificationService();
    $emailResult = $notification->sendPredictionEmail($prediction);

    if ($emailResult) {
        echo "   - 이메일 발송 성공!\n";
    } else {
        echo "   - 이메일 발송 실패. 설정을 확인하세요.\n";
    }

    echo "\n=== 크론잡 완료 ===\n";

} catch (Exception $e) {
    echo "\n!!! 오류 발생: " . $e->getMessage() . "\n";

    // 오류 알림 이메일 발송
    $notification = new NotificationService();
    $notification->sendSimpleAlert(
        '크론잡 오류',
        "비트코인 예측 크론잡에서 오류가 발생했습니다.\n\n오류: " . $e->getMessage()
    );
}
