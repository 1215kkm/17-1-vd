<?php
/**
 * 비트코인 예측 시스템 설정
 * 닷홈 호스팅용
 */

// 에러 표시 (운영 환경에서는 false)
define('DEBUG_MODE', true);

// 타임존 설정
date_default_timezone_set('Asia/Seoul');

// 이메일 설정 (사용자 설정 필요)
define('SMTP_HOST', 'smtp.gmail.com');
define('SMTP_PORT', 587);
define('SMTP_USER', 'your-email@gmail.com');        // 변경 필요
define('SMTP_PASS', 'your-app-password');           // Gmail 앱 비밀번호
define('NOTIFICATION_EMAIL', 'your-email@gmail.com'); // 알림 받을 이메일

// 카카오톡 알림 설정 (선택사항)
define('KAKAO_REST_API_KEY', '');                   // 카카오 REST API 키
define('KAKAO_REDIRECT_URI', '');                   // 리다이렉트 URI

// 데이터 캐시 경로
define('CACHE_DIR', __DIR__ . '/data/');
define('CACHE_FILE', CACHE_DIR . 'bitcoin_cache.json');
define('PREDICTION_FILE', CACHE_DIR . 'prediction_cache.json');

// API 설정
define('COINGECKO_API', 'https://api.coingecko.com/api/v3');
define('BINANCE_API', 'https://api.binance.com/api/v3');

// 캐시 유효시간 (초)
define('CACHE_TTL', 3600); // 1시간

// 에러 핸들링
if (DEBUG_MODE) {
    error_reporting(E_ALL);
    ini_set('display_errors', 1);
} else {
    error_reporting(0);
    ini_set('display_errors', 0);
}
