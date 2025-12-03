<?php
/**
 * 비트코인 데이터 수집 클래스
 */

class BitcoinData {
    private $cacheFile;
    private $cacheTTL;

    public function __construct() {
        $this->cacheFile = CACHE_FILE;
        $this->cacheTTL = CACHE_TTL;
    }

    /**
     * 캐시 디렉토리 확인 및 생성
     */
    private function ensureCacheDir() {
        $dir = dirname($this->cacheFile);
        if (!is_dir($dir)) {
            mkdir($dir, 0755, true);
        }
    }

    /**
     * 최근 3년 비트코인 데이터 가져오기
     */
    public function getHistoricalData($days = 1095) {
        $this->ensureCacheDir();

        // 캐시 확인
        if (file_exists($this->cacheFile)) {
            $cacheTime = filemtime($this->cacheFile);
            if ((time() - $cacheTime) < $this->cacheTTL) {
                $cached = json_decode(file_get_contents($this->cacheFile), true);
                if ($cached && count($cached) > 0) {
                    return $cached;
                }
            }
        }

        // Binance API에서 데이터 가져오기
        $prices = $this->fetchFromBinance($days);

        if (count($prices) > 0) {
            file_put_contents($this->cacheFile, json_encode($prices, JSON_PRETTY_PRINT));
        }

        return $prices;
    }

    /**
     * Binance API에서 일봉 데이터 가져오기
     */
    private function fetchFromBinance($days) {
        $prices = [];
        $endTime = time() * 1000;
        $limit = 1000;
        $iterations = ceil($days / $limit);

        for ($i = 0; $i < $iterations; $i++) {
            $url = BINANCE_API . "/klines?symbol=BTCUSDT&interval=1d&limit={$limit}&endTime={$endTime}";

            $response = $this->httpGet($url);
            if (!$response) break;

            $klines = json_decode($response, true);
            if (!$klines || count($klines) == 0) break;

            foreach ($klines as $kline) {
                $date = date('Y-m-d', $kline[0] / 1000);
                $prices[] = [
                    'date' => $date,
                    'timestamp' => $kline[0],
                    'open' => floatval($kline[1]),
                    'high' => floatval($kline[2]),
                    'low' => floatval($kline[3]),
                    'close' => floatval($kline[4]),
                    'volume' => floatval($kline[5])
                ];
            }

            // 다음 요청을 위해 가장 오래된 시간
            $endTime = $klines[0][0] - 1;
            usleep(100000); // API 제한 방지
        }

        // 날짜순 정렬
        usort($prices, function($a, $b) {
            return strcmp($a['date'], $b['date']);
        });

        return $prices;
    }

    /**
     * 현재 비트코인 가격
     */
    public function getCurrentPrice() {
        $url = COINGECKO_API . "/simple/price?ids=bitcoin&vs_currencies=usd,krw";
        $response = $this->httpGet($url);

        if ($response) {
            $data = json_decode($response, true);
            return [
                'usd' => $data['bitcoin']['usd'] ?? 0,
                'krw' => $data['bitcoin']['krw'] ?? 0
            ];
        }

        return ['usd' => 0, 'krw' => 0];
    }

    /**
     * 주간 데이터로 변환
     */
    public function convertToWeeklyData($dailyPrices) {
        $weekly = [];
        $weekGroups = [];

        foreach ($dailyPrices as $price) {
            $date = new DateTime($price['date']);
            $year = $date->format('Y');
            $week = $date->format('W');
            $key = "{$year}-{$week}";

            if (!isset($weekGroups[$key])) {
                $weekGroups[$key] = [];
            }
            $weekGroups[$key][] = $price;
        }

        foreach ($weekGroups as $key => $prices) {
            usort($prices, function($a, $b) {
                return strcmp($a['date'], $b['date']);
            });

            $first = $prices[0];
            $last = $prices[count($prices) - 1];

            $weekly[] = [
                'week_key' => $key,
                'week_start' => $first['date'],
                'week_end' => $last['date'],
                'open' => $first['open'],
                'close' => $last['close'],
                'high' => max(array_column($prices, 'high')),
                'low' => min(array_column($prices, 'low')),
                'change_percent' => $first['open'] != 0
                    ? (($last['close'] - $first['open']) / $first['open']) * 100
                    : 0,
                'is_up' => $last['close'] > $first['open'],
                'daily_prices' => $prices
            ];
        }

        return $weekly;
    }

    /**
     * HTTP GET 요청
     */
    private function httpGet($url) {
        $ch = curl_init();
        curl_setopt_array($ch, [
            CURLOPT_URL => $url,
            CURLOPT_RETURNTRANSFER => true,
            CURLOPT_TIMEOUT => 30,
            CURLOPT_HTTPHEADER => ['User-Agent: BitcoinPredictor/1.0'],
            CURLOPT_SSL_VERIFYPEER => false
        ]);

        $response = curl_exec($ch);
        $httpCode = curl_getinfo($ch, CURLINFO_HTTP_CODE);
        curl_close($ch);

        return ($httpCode == 200) ? $response : null;
    }
}
