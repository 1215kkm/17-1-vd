<?php
/**
 * 비트코인 예측 시스템 - 웹 인터페이스
 * 닷홈 호스팅용
 */

require_once __DIR__ . '/config.php';
require_once __DIR__ . '/includes/BitcoinData.php';
require_once __DIR__ . '/includes/PredictionEngine.php';

// 데이터 로드
$bitcoinData = new BitcoinData();
$predictionEngine = new PredictionEngine();

$currentPrice = $bitcoinData->getCurrentPrice();
$prices = $bitcoinData->getHistoricalData();
$weeklyData = $bitcoinData->convertToWeeklyData($prices);
$prediction = $predictionEngine->generatePrediction($prices, $weeklyData);

// 방향별 색상
$directionColors = [
    'Up' => '#4CAF50',
    'Down' => '#F44336',
    'Neutral' => '#FF9800'
];

$directionText = [
    'Up' => '📈 상승',
    'Down' => '📉 하락',
    'Neutral' => '➡️ 횡보'
];

$color = $directionColors[$prediction['direction']] ?? '#666';
$direction = $directionText[$prediction['direction']] ?? '알 수 없음';
?>
<!DOCTYPE html>
<html lang="ko">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>비트코인 주간 예측 시스템</title>
    <link href="https://fonts.googleapis.com/css2?family=Noto+Sans+KR:wght@400;500;700&display=swap" rel="stylesheet">
    <style>
        * {
            margin: 0;
            padding: 0;
            box-sizing: border-box;
        }

        body {
            font-family: 'Noto Sans KR', -apple-system, BlinkMacSystemFont, sans-serif;
            background: #121212;
            color: #fff;
            min-height: 100vh;
            padding: 20px;
        }

        .container {
            max-width: 1200px;
            margin: 0 auto;
        }

        .header {
            background: #1E1E1E;
            border-radius: 15px;
            padding: 25px 30px;
            margin-bottom: 20px;
            display: flex;
            justify-content: space-between;
            align-items: center;
            flex-wrap: wrap;
            gap: 15px;
        }

        .logo {
            display: flex;
            align-items: center;
            gap: 15px;
        }

        .logo-icon {
            font-size: 40px;
            color: #F7931A;
        }

        .logo-text h1 {
            font-size: 22px;
            font-weight: 700;
        }

        .logo-text p {
            font-size: 12px;
            color: #888;
        }

        .current-price {
            text-align: right;
        }

        .current-price .label {
            font-size: 12px;
            color: #888;
        }

        .current-price .price {
            font-size: 28px;
            font-weight: 700;
            color: #F7931A;
        }

        .cards {
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
            gap: 15px;
            margin-bottom: 20px;
        }

        .card {
            background: #1E1E1E;
            border-radius: 15px;
            padding: 25px;
            text-align: center;
        }

        .card .label {
            font-size: 13px;
            color: #888;
            margin-bottom: 10px;
        }

        .card .value {
            font-size: 28px;
            font-weight: 700;
        }

        .card .value.direction {
            color: <?= $color ?>;
        }

        .card .value.probability {
            color: #F7931A;
        }

        .main-content {
            display: grid;
            grid-template-columns: 1fr 1fr;
            gap: 20px;
        }

        @media (max-width: 900px) {
            .main-content {
                grid-template-columns: 1fr;
            }
        }

        .panel {
            background: #1E1E1E;
            border-radius: 15px;
            padding: 25px;
        }

        .panel h2 {
            font-size: 16px;
            font-weight: 600;
            margin-bottom: 20px;
            color: #F7931A;
        }

        .summary {
            background: #252525;
            border-radius: 10px;
            padding: 20px;
            margin-bottom: 20px;
            font-size: 15px;
            line-height: 1.6;
        }

        .reasons {
            list-style: none;
        }

        .reasons li {
            padding: 10px 0;
            border-bottom: 1px solid #333;
            font-size: 13px;
            color: #ccc;
        }

        .reasons li:last-child {
            border-bottom: none;
        }

        .patterns-table {
            width: 100%;
            border-collapse: collapse;
            font-size: 12px;
        }

        .patterns-table th {
            background: #252525;
            padding: 12px 10px;
            text-align: left;
            font-weight: 500;
            color: #888;
        }

        .patterns-table td {
            padding: 12px 10px;
            border-bottom: 1px solid #333;
            color: #ccc;
        }

        .patterns-table tr:last-child td {
            border-bottom: none;
        }

        .matched {
            color: #4CAF50;
        }

        .not-matched {
            color: #666;
        }

        .indicators {
            display: grid;
            grid-template-columns: repeat(2, 1fr);
            gap: 15px;
        }

        .indicator {
            background: #252525;
            border-radius: 10px;
            padding: 15px;
        }

        .indicator .label {
            font-size: 11px;
            color: #888;
            margin-bottom: 5px;
        }

        .indicator .value {
            font-size: 16px;
            font-weight: 600;
        }

        .footer {
            text-align: center;
            padding: 30px;
            color: #666;
            font-size: 11px;
        }

        .footer a {
            color: #F7931A;
            text-decoration: none;
        }

        .refresh-btn {
            background: #F7931A;
            color: #000;
            border: none;
            padding: 12px 25px;
            border-radius: 8px;
            font-weight: 600;
            cursor: pointer;
            font-size: 14px;
            transition: opacity 0.2s;
        }

        .refresh-btn:hover {
            opacity: 0.8;
        }

        .update-time {
            font-size: 11px;
            color: #666;
            margin-top: 10px;
        }
    </style>
</head>
<body>
    <div class="container">
        <!-- 헤더 -->
        <div class="header">
            <div class="logo">
                <span class="logo-icon">₿</span>
                <div class="logo-text">
                    <h1>비트코인 주간 예측 시스템</h1>
                    <p>3년간 패턴 분석 기반 AI 예측</p>
                </div>
            </div>
            <div class="current-price">
                <div class="label">현재 가격</div>
                <div class="price">$<?= number_format($currentPrice['usd'], 0) ?></div>
            </div>
        </div>

        <!-- 예측 결과 카드 -->
        <div class="cards">
            <div class="card">
                <div class="label">이번 주 예측</div>
                <div class="value direction"><?= $direction ?></div>
            </div>
            <div class="card">
                <div class="label">예측 확률</div>
                <div class="value probability"><?= number_format($prediction['probability'], 1) ?>%</div>
            </div>
            <div class="card">
                <div class="label">신뢰도</div>
                <div class="value"><?= number_format($prediction['confidence'], 0) ?>%</div>
            </div>
            <div class="card">
                <div class="label">분석 기간</div>
                <div class="value" style="font-size: 18px;"><?= count($prices) ?>일</div>
            </div>
        </div>

        <!-- 메인 컨텐츠 -->
        <div class="main-content">
            <!-- 예측 상세 -->
            <div class="panel">
                <h2>📊 예측 상세</h2>
                <div class="summary">
                    <?= htmlspecialchars($prediction['summary']) ?>
                </div>

                <h2 style="margin-top: 25px;">💡 분석 근거</h2>
                <ul class="reasons">
                    <?php foreach ($prediction['reasons'] as $reason): ?>
                        <?php if (!empty($reason)): ?>
                            <li><?= htmlspecialchars($reason) ?></li>
                        <?php endif; ?>
                    <?php endforeach; ?>
                </ul>
            </div>

            <!-- 패턴 분석 -->
            <div class="panel">
                <h2>🔍 패턴 분석 결과</h2>
                <table class="patterns-table">
                    <thead>
                        <tr>
                            <th>패턴</th>
                            <th>설명</th>
                            <th>상승확률</th>
                            <th>신뢰도</th>
                        </tr>
                    </thead>
                    <tbody>
                        <?php foreach ($prediction['patterns'] as $pattern): ?>
                            <tr class="<?= $pattern['is_matched'] ? 'matched' : 'not-matched' ?>">
                                <td>
                                    <?= $pattern['is_matched'] ? '✅' : '⬜' ?>
                                    <?= htmlspecialchars($pattern['name']) ?>
                                </td>
                                <td><?= htmlspecialchars($pattern['description']) ?></td>
                                <td><?= number_format($pattern['up_probability'], 1) ?>%</td>
                                <td><?= number_format($pattern['confidence'], 0) ?>%</td>
                            </tr>
                        <?php endforeach; ?>
                    </tbody>
                </table>

                <h2 style="margin-top: 30px;">📈 기술적 지표</h2>
                <div class="indicators">
                    <div class="indicator">
                        <div class="label">RSI (14일)</div>
                        <div class="value"><?= number_format($prediction['indicators']['rsi'], 1) ?></div>
                    </div>
                    <div class="indicator">
                        <div class="label">SMA 7일</div>
                        <div class="value">$<?= number_format($prediction['indicators']['sma7'], 0) ?></div>
                    </div>
                    <div class="indicator">
                        <div class="label">SMA 25일</div>
                        <div class="value">$<?= number_format($prediction['indicators']['sma25'], 0) ?></div>
                    </div>
                    <div class="indicator">
                        <div class="label">볼린저 밴드</div>
                        <div class="value" style="font-size: 12px;">
                            $<?= number_format($prediction['indicators']['bollinger_lower'], 0) ?> ~
                            $<?= number_format($prediction['indicators']['bollinger_upper'], 0) ?>
                        </div>
                    </div>
                </div>

                <div style="margin-top: 25px; text-align: center;">
                    <button class="refresh-btn" onclick="location.reload()">🔄 새로고침</button>
                    <div class="update-time">
                        마지막 업데이트: <?= date('Y-m-d H:i:s') ?>
                    </div>
                </div>
            </div>
        </div>

        <!-- 푸터 -->
        <div class="footer">
            <p>⚠️ 본 예측은 과거 데이터 기반 패턴 분석이며, 투자 조언이 아닙니다.</p>
            <p>투자의 책임은 본인에게 있습니다.</p>
            <p style="margin-top: 15px;">
                예측 대상 기간: <?= $prediction['target_week_start'] ?> ~ <?= $prediction['target_week_end'] ?>
            </p>
        </div>
    </div>
</body>
</html>
