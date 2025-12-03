using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using BitcoinPredictor.Models;
using BitcoinPredictor.Services;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;

namespace BitcoinPredictor.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    private readonly BitcoinDataService _dataService;
    private readonly PredictionEngine _predictionEngine;

    private List<BitcoinPrice> _prices = new();
    private List<WeeklyData> _weeklyData = new();

    public MainViewModel()
    {
        _dataService = new BitcoinDataService();
        _predictionEngine = new PredictionEngine();

        Patterns = new ObservableCollection<PatternAnalysis>();
        Reasons = new ObservableCollection<string>();

        LoadDataCommand = new RelayCommand(async () => await LoadDataAsync());
        RefreshCommand = new RelayCommand(async () => await RefreshPredictionAsync());
        BacktestCommand = new RelayCommand(async () => await RunBacktestAsync());

        // 초기 로드
        _ = LoadDataAsync();
    }

    #region Properties

    private string _currentPrice = "로딩 중...";
    public string CurrentPrice
    {
        get => _currentPrice;
        set { _currentPrice = value; OnPropertyChanged(); }
    }

    private string _predictionDirection = "-";
    public string PredictionDirection
    {
        get => _predictionDirection;
        set { _predictionDirection = value; OnPropertyChanged(); }
    }

    private string _predictionProbability = "0%";
    public string PredictionProbability
    {
        get => _predictionProbability;
        set { _predictionProbability = value; OnPropertyChanged(); }
    }

    private string _predictionConfidence = "신뢰도: -";
    public string PredictionConfidence
    {
        get => _predictionConfidence;
        set { _predictionConfidence = value; OnPropertyChanged(); }
    }

    private string _predictionSummary = "";
    public string PredictionSummary
    {
        get => _predictionSummary;
        set { _predictionSummary = value; OnPropertyChanged(); }
    }

    private string _statusMessage = "준비 중...";
    public string StatusMessage
    {
        get => _statusMessage;
        set { _statusMessage = value; OnPropertyChanged(); }
    }

    private string _backtestResult = "";
    public string BacktestResult
    {
        get => _backtestResult;
        set { _backtestResult = value; OnPropertyChanged(); }
    }

    private string _lastUpdate = "";
    public string LastUpdate
    {
        get => _lastUpdate;
        set { _lastUpdate = value; OnPropertyChanged(); }
    }

    private bool _isLoading;
    public bool IsLoading
    {
        get => _isLoading;
        set { _isLoading = value; OnPropertyChanged(); }
    }

    private string _directionColor = "#666666";
    public string DirectionColor
    {
        get => _directionColor;
        set { _directionColor = value; OnPropertyChanged(); }
    }

    public ObservableCollection<PatternAnalysis> Patterns { get; }
    public ObservableCollection<string> Reasons { get; }

    // 차트 데이터
    private ISeries[] _priceSeries = Array.Empty<ISeries>();
    public ISeries[] PriceSeries
    {
        get => _priceSeries;
        set { _priceSeries = value; OnPropertyChanged(); }
    }

    private Axis[] _xAxes = Array.Empty<Axis>();
    public Axis[] XAxes
    {
        get => _xAxes;
        set { _xAxes = value; OnPropertyChanged(); }
    }

    private Axis[] _yAxes = Array.Empty<Axis>();
    public Axis[] YAxes
    {
        get => _yAxes;
        set { _yAxes = value; OnPropertyChanged(); }
    }

    // RSI 지표
    private string _rsiValue = "RSI: -";
    public string RsiValue
    {
        get => _rsiValue;
        set { _rsiValue = value; OnPropertyChanged(); }
    }

    private string _smaStatus = "이동평균: -";
    public string SmaStatus
    {
        get => _smaStatus;
        set { _smaStatus = value; OnPropertyChanged(); }
    }

    private string _bollingerStatus = "볼린저: -";
    public string BollingerStatus
    {
        get => _bollingerStatus;
        set { _bollingerStatus = value; OnPropertyChanged(); }
    }

    #endregion

    #region Commands

    public ICommand LoadDataCommand { get; }
    public ICommand RefreshCommand { get; }
    public ICommand BacktestCommand { get; }

    #endregion

    #region Methods

    private async Task LoadDataAsync()
    {
        try
        {
            IsLoading = true;
            StatusMessage = "비트코인 데이터 수집 중... (최대 1분 소요)";

            // 현재 가격 가져오기
            var currentPriceValue = await _dataService.GetCurrentPriceAsync();
            CurrentPrice = currentPriceValue > 0 ? $"${currentPriceValue:N0}" : "가격 정보 없음";

            // 과거 데이터 가져오기
            StatusMessage = "3년간 과거 데이터 분석 중...";
            _prices = await _dataService.GetHistoricalDataAsync();
            _weeklyData = _dataService.ConvertToWeeklyData(_prices);

            StatusMessage = "예측 생성 중...";
            await RefreshPredictionAsync();

            // 차트 업데이트
            UpdateChart();

            LastUpdate = $"마지막 업데이트: {DateTime.Now:yyyy-MM-dd HH:mm}";
            StatusMessage = $"완료 - {_prices.Count}일 데이터 분석됨";
        }
        catch (Exception ex)
        {
            StatusMessage = $"오류: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task RefreshPredictionAsync()
    {
        if (_prices.Count == 0)
        {
            await LoadDataAsync();
            return;
        }

        try
        {
            IsLoading = true;
            StatusMessage = "예측 생성 중...";

            var prediction = _predictionEngine.GeneratePrediction(_prices, _weeklyData);

            // UI 업데이트
            PredictionDirection = prediction.Direction switch
            {
                "Up" => "📈 상승",
                "Down" => "📉 하락",
                _ => "➡️ 횡보"
            };

            DirectionColor = prediction.Direction switch
            {
                "Up" => "#4CAF50",
                "Down" => "#F44336",
                _ => "#FF9800"
            };

            PredictionProbability = $"{prediction.Probability:F1}%";
            PredictionConfidence = $"신뢰도: {prediction.Confidence:F0}%";
            PredictionSummary = prediction.Summary;

            // 패턴 목록 업데이트
            Patterns.Clear();
            foreach (var pattern in prediction.MatchedPatterns.OrderByDescending(p => p.Confidence))
            {
                Patterns.Add(pattern);
            }

            // 이유 목록 업데이트
            Reasons.Clear();
            foreach (var reason in prediction.Reasons)
            {
                Reasons.Add(reason);
            }

            // 기술적 지표 업데이트
            var ind = prediction.CurrentIndicators;
            RsiValue = $"RSI: {ind.RSI:F1}";
            SmaStatus = $"SMA7: ${ind.SMA7:N0} | SMA25: ${ind.SMA25:N0}";
            BollingerStatus = $"BB: ${ind.BollingerLower:N0} ~ ${ind.BollingerUpper:N0}";

            StatusMessage = "예측 완료";
        }
        catch (Exception ex)
        {
            StatusMessage = $"예측 오류: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task RunBacktestAsync()
    {
        if (_prices.Count == 0)
        {
            BacktestResult = "먼저 데이터를 로드해주세요.";
            return;
        }

        try
        {
            IsLoading = true;
            StatusMessage = "백테스트 실행 중... (최근 52주)";

            await Task.Run(() =>
            {
                var result = _predictionEngine.RunBacktest(_prices, _weeklyData, 52);
                BacktestResult = result.Summary;
            });

            StatusMessage = "백테스트 완료";
        }
        catch (Exception ex)
        {
            BacktestResult = $"백테스트 오류: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void UpdateChart()
    {
        if (_prices.Count == 0) return;

        // 최근 90일 데이터만 표시
        var recentPrices = _prices.TakeLast(90).ToList();

        var values = recentPrices.Select(p => (double)p.Close).ToArray();
        var dates = recentPrices.Select(p => p.Date).ToArray();

        PriceSeries = new ISeries[]
        {
            new LineSeries<double>
            {
                Values = values,
                Fill = new SolidColorPaint(SKColors.Orange.WithAlpha(50)),
                Stroke = new SolidColorPaint(SKColors.Orange, 2),
                GeometrySize = 0,
                LineSmoothness = 0.5
            }
        };

        XAxes = new Axis[]
        {
            new Axis
            {
                Labels = dates.Select((d, i) => i % 10 == 0 ? d.ToString("MM/dd") : "").ToArray(),
                LabelsRotation = 45,
                TextSize = 10
            }
        };

        YAxes = new Axis[]
        {
            new Axis
            {
                Labeler = v => $"${v:N0}",
                TextSize = 10
            }
        };
    }

    #endregion

    #region INotifyPropertyChanged

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    #endregion
}

public class RelayCommand : ICommand
{
    private readonly Func<Task> _execute;
    private readonly Func<bool>? _canExecute;

    public RelayCommand(Func<Task> execute, Func<bool>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;

    public async void Execute(object? parameter) => await _execute();
}
