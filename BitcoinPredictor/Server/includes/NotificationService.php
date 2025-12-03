<?php
/**
 * 알림 서비스 (이메일 / 카카오톡)
 */

class NotificationService {

    /**
     * 이메일 발송
     */
    public function sendEmail($to, $subject, $body) {
        // PHP mail() 함수 사용 (기본)
        // 닷홈에서는 mail() 함수 사용 가능

        $headers = [
            'MIME-Version: 1.0',
            'Content-type: text/html; charset=utf-8',
            'From: Bitcoin Predictor <noreply@yourdomain.com>',
            'X-Mailer: PHP/' . phpversion()
        ];

        $htmlBody = $this->createEmailTemplate($subject, $body);

        return mail($to, $subject, $htmlBody, implode("\r\n", $headers));
    }

    /**
     * 예측 결과 이메일 발송
     */
    public function sendPredictionEmail($prediction) {
        $to = NOTIFICATION_EMAIL;

        $directionEmoji = [
            'Up' => '📈',
            'Down' => '📉',
            'Neutral' => '➡️'
        ];

        $directionText = [
            'Up' => '상승',
            'Down' => '하락',
            'Neutral' => '횡보'
        ];

        $emoji = $directionEmoji[$prediction['direction']] ?? '❓';
        $direction = $directionText[$prediction['direction']] ?? '알 수 없음';

        $subject = "{$emoji} [비트코인 주간 예측] 이번 주 {$direction} 예상 ({$prediction['probability']}%)";

        $body = $this->formatPredictionBody($prediction);

        return $this->sendEmail($to, $subject, $body);
    }

    /**
     * 예측 결과 본문 생성
     */
    private function formatPredictionBody($prediction) {
        $directionColor = [
            'Up' => '#4CAF50',
            'Down' => '#F44336',
            'Neutral' => '#FF9800'
        ];

        $directionText = [
            'Up' => '상승',
            'Down' => '하락',
            'Neutral' => '횡보'
        ];

        $color = $directionColor[$prediction['direction']] ?? '#666666';
        $direction = $directionText[$prediction['direction']] ?? '알 수 없음';

        $currentPrice = number_format($prediction['current_price'] ?? 0, 0);
        $probability = number_format($prediction['probability'], 1);
        $confidence = number_format($prediction['confidence'], 0);

        $reasons = '';
        foreach ($prediction['reasons'] as $reason) {
            if (empty($reason)) {
                $reasons .= '<br>';
            } else {
                $reasons .= "<li>{$reason}</li>";
            }
        }

        $patterns = '';
        foreach ($prediction['patterns'] as $pattern) {
            $matchIcon = $pattern['is_matched'] ? '✅' : '❌';
            $patterns .= sprintf(
                '<tr><td>%s %s</td><td>%s</td><td>%.1f%%</td><td>%.0f%%</td></tr>',
                $matchIcon,
                htmlspecialchars($pattern['name']),
                htmlspecialchars($pattern['description']),
                $pattern['up_probability'],
                $pattern['confidence']
            );
        }

        return <<<HTML
<div style="max-width: 600px; margin: 0 auto; font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif;">
    <!-- 헤더 -->
    <div style="background: linear-gradient(135deg, #1E1E1E 0%, #2D2D2D 100%); padding: 30px; border-radius: 10px 10px 0 0;">
        <h1 style="color: #F7931A; margin: 0; font-size: 24px;">₿ 비트코인 주간 예측</h1>
        <p style="color: #888; margin: 5px 0 0 0; font-size: 14px;">
            {$prediction['target_week_start']} ~ {$prediction['target_week_end']}
        </p>
    </div>

    <!-- 예측 결과 -->
    <div style="background: #1E1E1E; padding: 30px;">
        <div style="display: flex; justify-content: space-around; text-align: center; margin-bottom: 30px;">
            <div style="flex: 1; padding: 20px; background: #252525; border-radius: 10px; margin: 0 5px;">
                <p style="color: #888; margin: 0 0 10px 0; font-size: 12px;">예측 방향</p>
                <p style="color: {$color}; margin: 0; font-size: 28px; font-weight: bold;">{$direction}</p>
            </div>
            <div style="flex: 1; padding: 20px; background: #252525; border-radius: 10px; margin: 0 5px;">
                <p style="color: #888; margin: 0 0 10px 0; font-size: 12px;">확률</p>
                <p style="color: #F7931A; margin: 0; font-size: 28px; font-weight: bold;">{$probability}%</p>
            </div>
            <div style="flex: 1; padding: 20px; background: #252525; border-radius: 10px; margin: 0 5px;">
                <p style="color: #888; margin: 0 0 10px 0; font-size: 12px;">신뢰도</p>
                <p style="color: #fff; margin: 0; font-size: 28px; font-weight: bold;">{$confidence}%</p>
            </div>
        </div>

        <div style="background: #252525; padding: 20px; border-radius: 10px; margin-bottom: 20px;">
            <p style="color: #F7931A; margin: 0 0 10px 0; font-size: 16px; font-weight: bold;">현재 가격</p>
            <p style="color: #fff; margin: 0; font-size: 24px;">\${$currentPrice} USD</p>
        </div>

        <!-- 분석 이유 -->
        <div style="background: #252525; padding: 20px; border-radius: 10px; margin-bottom: 20px;">
            <p style="color: #F7931A; margin: 0 0 15px 0; font-size: 16px; font-weight: bold;">분석 근거</p>
            <ul style="color: #ccc; margin: 0; padding-left: 20px; line-height: 1.8;">
                {$reasons}
            </ul>
        </div>

        <!-- 패턴 분석 테이블 -->
        <div style="background: #252525; padding: 20px; border-radius: 10px;">
            <p style="color: #F7931A; margin: 0 0 15px 0; font-size: 16px; font-weight: bold;">패턴 분석 상세</p>
            <table style="width: 100%; color: #ccc; font-size: 12px; border-collapse: collapse;">
                <tr style="background: #333;">
                    <th style="padding: 10px; text-align: left;">패턴</th>
                    <th style="padding: 10px; text-align: left;">설명</th>
                    <th style="padding: 10px; text-align: center;">상승확률</th>
                    <th style="padding: 10px; text-align: center;">신뢰도</th>
                </tr>
                {$patterns}
            </table>
        </div>
    </div>

    <!-- 푸터 -->
    <div style="background: #151515; padding: 20px; border-radius: 0 0 10px 10px; text-align: center;">
        <p style="color: #666; margin: 0; font-size: 11px;">
            ⚠️ 본 예측은 과거 데이터 기반 패턴 분석이며, 투자 조언이 아닙니다.<br>
            투자의 책임은 본인에게 있습니다.
        </p>
        <p style="color: #444; margin: 10px 0 0 0; font-size: 10px;">
            생성 시간: {$prediction['prediction_date']}
        </p>
    </div>
</div>
HTML;
    }

    /**
     * 이메일 템플릿
     */
    private function createEmailTemplate($subject, $body) {
        return <<<HTML
<!DOCTYPE html>
<html>
<head>
    <meta charset="utf-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>{$subject}</title>
</head>
<body style="margin: 0; padding: 20px; background: #121212;">
    {$body}
</body>
</html>
HTML;
    }

    /**
     * 카카오톡 알림 발송 (카카오 비즈니스 계정 필요)
     * 개인 계정으로는 나에게 보내기만 가능
     */
    public function sendKakaoMessage($message) {
        if (empty(KAKAO_REST_API_KEY)) {
            return false;
        }

        // 카카오 API는 OAuth 토큰이 필요
        // 아래는 예시 코드 (실제 사용시 토큰 관리 필요)

        /*
        $accessToken = $this->getKakaoAccessToken();

        $url = 'https://kapi.kakao.com/v2/api/talk/memo/default/send';
        $data = [
            'template_object' => json_encode([
                'object_type' => 'text',
                'text' => $message,
                'link' => [
                    'web_url' => 'https://yourdomain.com',
                    'mobile_web_url' => 'https://yourdomain.com'
                ]
            ])
        ];

        $ch = curl_init();
        curl_setopt_array($ch, [
            CURLOPT_URL => $url,
            CURLOPT_POST => true,
            CURLOPT_POSTFIELDS => http_build_query($data),
            CURLOPT_RETURNTRANSFER => true,
            CURLOPT_HTTPHEADER => [
                'Authorization: Bearer ' . $accessToken,
                'Content-Type: application/x-www-form-urlencoded'
            ]
        ]);

        $response = curl_exec($ch);
        curl_close($ch);

        return $response !== false;
        */

        return false;
    }

    /**
     * 간단한 텍스트 알림 이메일
     */
    public function sendSimpleAlert($subject, $message) {
        $to = NOTIFICATION_EMAIL;

        $body = <<<HTML
<div style="max-width: 500px; margin: 0 auto; font-family: Arial, sans-serif; background: #1E1E1E; padding: 30px; border-radius: 10px;">
    <h2 style="color: #F7931A; margin: 0 0 20px 0;">₿ {$subject}</h2>
    <p style="color: #ccc; line-height: 1.6; white-space: pre-wrap;">{$message}</p>
    <hr style="border: none; border-top: 1px solid #333; margin: 20px 0;">
    <p style="color: #666; font-size: 11px; margin: 0;">
        발송 시간: " . date('Y-m-d H:i:s') . "
    </p>
</div>
HTML;

        return $this->sendEmail($to, "[비트코인 알림] " . $subject, $body);
    }
}
