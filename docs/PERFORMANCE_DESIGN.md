# WebFlux 성능 설계

> 엔터프라이즈급 성능 달성을 위한 종합적 최적화 전략

## 🎯 성능 목표

연구 문서와 README에서 제시한 **검증된 성능 지표**를 달성하기 위한 설계입니다.

### 핵심 성능 지표

| 메트릭 | 목표값 | 측정 기준 |
|--------|--------|-----------|
| **크롤링 속도** | 100페이지/분 | 평균 1MB 페이지 기준 |
| **메모리 효율** | 페이지 크기 1.5배 이하 | 동시 처리 중 메모리 사용량 |
| **품질 보장** | 청크 완성도 81%+ | 자동 품질 평가 기준 |
| **컨텍스트 보존** | 75%+ | 의미론적 연관성 유지 |
| **병렬 확장** | CPU 코어 수 선형 증가 | 코어별 성능 향상 |
| **MemoryOptimized** | 84% 메모리 절감 | 기본 전략 대비 |

### 아키텍처 성능 원칙

1. **병렬 우선**: CPU 코어 활용 극대화
2. **스트리밍 최적화**: 메모리 효율성 우선
3. **백프레셔 제어**: 시스템 안정성 보장
4. **지능형 캐싱**: 중복 작업 최소화
5. **동적 확장**: 리소스에 따른 적응적 처리

## 🚀 병렬 처리 엔진 설계

### CPU 코어별 동적 스케일링

```csharp
public class DynamicParallelProcessingEngine : IParallelProcessingEngine
{
    private readonly int _coreCount;
    private readonly Channel<WorkItem> _workQueue;
    private readonly SemaphoreSlim[] _workerSemaphores;
    private readonly PerformanceMonitor _performanceMonitor;
    private readonly CancellationTokenSource _cancellationTokenSource;

    public DynamicParallelProcessingEngine(ParallelProcessingOptions options)
    {
        _coreCount = options.MaxWorkers ?? Environment.ProcessorCount;
        _workQueue = Channel.CreateBounded<WorkItem>(new BoundedChannelOptions(options.QueueCapacity)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = false,
            SingleWriter = false
        });

        _workerSemaphores = new SemaphoreSlim[_coreCount];
        for (int i = 0; i < _coreCount; i++)
        {
            _workerSemaphores[i] = new SemaphoreSlim(1, 1);
        }

        _performanceMonitor = new PerformanceMonitor();
        InitializeWorkers();
    }

    private void InitializeWorkers()
    {
        for (int workerId = 0; workerId < _coreCount; workerId++)
        {
            var localWorkerId = workerId;
            _ = Task.Run(async () => await WorkerLoop(localWorkerId));
        }
    }

    private async Task WorkerLoop(int workerId)
    {
        var workerMetrics = new WorkerMetrics(workerId);

        while (!_cancellationTokenSource.Token.IsCancellationRequested)
        {
            try
            {
                await foreach (var workItem in _workQueue.Reader.ReadAllAsync(_cancellationTokenSource.Token))
                {
                    await ProcessWorkItem(workItem, workerId, workerMetrics);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                // 워커 레벨 오류 처리
                await HandleWorkerError(workerId, ex);
            }
        }
    }

    private async Task ProcessWorkItem(WorkItem workItem, int workerId, WorkerMetrics metrics)
    {
        var semaphore = _workerSemaphores[workerId];
        await semaphore.WaitAsync();

        var stopwatch = Stopwatch.StartNew();
        try
        {
            // 백프레셔 체크
            if (await ShouldThrottleWorker(workerId))
            {
                await Task.Delay(100); // 백프레셔 지연
            }

            await workItem.ExecuteAsync(_cancellationTokenSource.Token);

            stopwatch.Stop();
            metrics.RecordSuccess(stopwatch.Elapsed);
            _performanceMonitor.RecordWorkItem(workerId, stopwatch.Elapsed, true);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            metrics.RecordFailure(stopwatch.Elapsed, ex);
            _performanceMonitor.RecordWorkItem(workerId, stopwatch.Elapsed, false);

            // 재시도 로직
            if (workItem.CanRetry)
            {
                workItem.IncrementRetryCount();
                await _workQueue.Writer.WriteAsync(workItem);
            }
        }
        finally
        {
            semaphore.Release();
        }
    }

    public async Task<ProcessingResult<T>> ProcessBatchAsync<T>(
        IEnumerable<IWorkItem<T>> workItems,
        CancellationToken cancellationToken = default)
    {
        var results = new ConcurrentBag<T>();
        var exceptions = new ConcurrentBag<Exception>();
        var totalItems = workItems.Count();

        var progress = new Progress<int>(completed =>
        {
            var percentage = (double)completed / totalItems * 100;
            _performanceMonitor.UpdateProgress(percentage);
        });

        var completedCount = 0;
        var tasks = workItems.Select(async workItem =>
        {
            try
            {
                var result = await ProcessSingleWorkItem(workItem, cancellationToken);
                results.Add(result);
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
            finally
            {
                Interlocked.Increment(ref completedCount);
                progress.Report(completedCount);
            }
        });

        await Task.WhenAll(tasks);

        return new ProcessingResult<T>
        {
            Results = results.ToList(),
            Exceptions = exceptions.ToList(),
            TotalProcessed = completedCount,
            SuccessRate = (double)(totalItems - exceptions.Count) / totalItems
        };
    }

    private async Task<bool> ShouldThrottleWorker(int workerId)
    {
        var cpuUsage = await _performanceMonitor.GetCpuUsageAsync();
        var memoryUsage = await _performanceMonitor.GetMemoryUsageAsync();

        // CPU 사용률이 95% 이상이거나 메모리 사용률이 85% 이상이면 백프레셔
        return cpuUsage > 95.0 || memoryUsage > 0.85;
    }
}

public class ParallelProcessingOptions
{
    public int? MaxWorkers { get; set; }
    public int QueueCapacity { get; set; } = 1000;
    public TimeSpan WorkerIdleTimeout { get; set; } = TimeSpan.FromMinutes(5);
    public bool EnableDynamicScaling { get; set; } = true;
    public double CpuThrottleThreshold { get; set; } = 95.0;
    public double MemoryThrottleThreshold { get; set; } = 0.85;
}
```

### 작업 분산 및 로드 밸런싱

```csharp
public class WorkItemDistributor
{
    private readonly IWorkItemPriorityCalculator _priorityCalculator;
    private readonly ILoadBalancer _loadBalancer;

    public async Task<List<WorkerAssignment>> DistributeWorkItems(
        IEnumerable<WorkItem> workItems,
        int availableWorkers,
        WorkerCapacity[] workerCapacities)
    {
        var assignments = new List<WorkerAssignment>();

        // 1. 작업 우선순위 계산
        var prioritizedItems = workItems
            .Select(item => new PrioritizedWorkItem
            {
                Item = item,
                Priority = _priorityCalculator.Calculate(item),
                EstimatedDuration = EstimateProcessingTime(item)
            })
            .OrderByDescending(p => p.Priority)
            .ToList();

        // 2. 워커별 용량 기반 분배
        for (int workerId = 0; workerId < availableWorkers; workerId++)
        {
            var workerCapacity = workerCapacities[workerId];
            var assignment = new WorkerAssignment { WorkerId = workerId };

            var remainingCapacity = workerCapacity.MaxCapacity;

            foreach (var prioritizedItem in prioritizedItems)
            {
                if (prioritizedItem.IsAssigned)
                    continue;

                if (prioritizedItem.EstimatedDuration <= remainingCapacity)
                {
                    assignment.WorkItems.Add(prioritizedItem.Item);
                    prioritizedItem.IsAssigned = true;
                    remainingCapacity -= prioritizedItem.EstimatedDuration;
                }
            }

            assignments.Add(assignment);
        }

        // 3. 미할당 작업 재분배
        await RedistributeUnassignedItems(prioritizedItems.Where(p => !p.IsAssigned), assignments);

        return assignments;
    }

    private TimeSpan EstimateProcessingTime(WorkItem workItem)
    {
        return workItem.Type switch
        {
            WorkItemType.WebCrawl => TimeSpan.FromSeconds(2), // 평균 크롤링 시간
            WorkItemType.ContentExtraction => TimeSpan.FromSeconds(1), // 평균 추출 시간
            WorkItemType.Parsing => TimeSpan.FromSeconds(0.5), // 평균 파싱 시간
            WorkItemType.Chunking => TimeSpan.FromSeconds(0.3), // 평균 청킹 시간
            WorkItemType.ImageProcessing => TimeSpan.FromSeconds(5), // 평균 이미지 처리 시간
            _ => TimeSpan.FromSeconds(1)
        };
    }
}

public class WorkerCapacity
{
    public int WorkerId { get; set; }
    public TimeSpan MaxCapacity { get; set; }
    public TimeSpan CurrentLoad { get; set; }
    public double CpuUtilization { get; set; }
    public long MemoryUsage { get; set; }
    public int ActiveTasks { get; set; }

    public TimeSpan AvailableCapacity => MaxCapacity - CurrentLoad;
    public bool CanAcceptWork => AvailableCapacity > TimeSpan.Zero && CpuUtilization < 90;
}
```

## 📊 스트리밍 최적화

### AsyncEnumerable 기반 실시간 처리

```csharp
public class StreamingWebContentProcessor : IWebContentProcessor
{
    private readonly ICrawlerFactory _crawlerFactory;
    private readonly IContentExtractorFactory _extractorFactory;
    private readonly IChunkingStrategyFactory _chunkingFactory;
    private readonly StreamingConfiguration _config;

    public async IAsyncEnumerable<ProcessingResult<IEnumerable<WebContentChunk>>>
        ProcessWithProgressAsync(
            string baseUrl,
            CrawlOptions? crawlOptions = null,
            ChunkingOptions? chunkingOptions = null,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using var streamingContext = new StreamingProcessingContext(_config);

        // 1. 스트리밍 파이프라인 설정
        var pipeline = CreateStreamingPipeline(baseUrl, crawlOptions, chunkingOptions, streamingContext);

        // 2. 실시간 청크 생성 및 반환
        var chunkCount = 0;
        await foreach (var chunkBatch in pipeline.WithCancellation(cancellationToken))
        {
            chunkCount += chunkBatch.Count();

            // 메모리 압박 체크
            if (chunkCount % 50 == 0) // 50개 청크마다 체크
            {
                await streamingContext.CheckMemoryPressureAsync();
            }

            yield return ProcessingResult<IEnumerable<WebContentChunk>>.Success(
                chunkBatch,
                streamingContext.GetCurrentProgress()
            );

            // 백프레셔 적용
            if (await streamingContext.ShouldApplyBackpressureAsync())
            {
                await Task.Delay(100, cancellationToken);
            }
        }
    }

    private async IAsyncEnumerable<IEnumerable<WebContentChunk>> CreateStreamingPipeline(
        string baseUrl,
        CrawlOptions? crawlOptions,
        ChunkingOptions? chunkingOptions,
        StreamingProcessingContext context,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // 스트리밍 크롤링
        var crawler = _crawlerFactory.CreateOptimalCrawler(baseUrl, crawlOptions ?? new CrawlOptions());

        await foreach (var crawlResult in crawler.CrawlStreamAsync(baseUrl, crawlOptions, cancellationToken))
        {
            if (crawlResult.Error != null)
            {
                context.RecordError(crawlResult.Error);
                continue;
            }

            // 스트리밍 추출 및 청킹
            var chunks = await ProcessSinglePageStreamingAsync(crawlResult, chunkingOptions, context, cancellationToken);

            if (chunks.Any())
            {
                yield return chunks;
            }
        }
    }

    private async Task<IEnumerable<WebContentChunk>> ProcessSinglePageStreamingAsync(
        CrawlResult crawlResult,
        ChunkingOptions? chunkingOptions,
        StreamingProcessingContext context,
        CancellationToken cancellationToken)
    {
        try
        {
            // 1. 콘텐츠 추출 (스트리밍)
            var extractor = _extractorFactory.GetExtractor(crawlResult.ContentType, crawlResult.Url);
            using var httpClient = context.HttpClientFactory.CreateClient();
            var response = await httpClient.GetAsync(crawlResult.Url, cancellationToken);
            var rawContent = await extractor.ExtractAsync(crawlResult.Url, response, cancellationToken);

            // 2. 파싱 (메모리 효율적)
            var parser = new MemoryEfficientContentParser();
            var parsedContent = await parser.ParseAsync(rawContent, cancellationToken);

            // 3. 스트리밍 청킹
            var chunkingStrategy = _chunkingFactory.CreateOptimalStrategy(parsedContent, chunkingOptions ?? new ChunkingOptions());

            var chunks = new List<WebContentChunk>();
            if (chunkingStrategy is IStreamingChunkingStrategy streamingStrategy)
            {
                await foreach (var chunk in streamingStrategy.ChunkStreamAsync(parsedContent, chunkingOptions, cancellationToken))
                {
                    chunks.Add(chunk);

                    // 메모리 관리: 청크가 많아지면 중간 반환
                    if (chunks.Count >= _config.MaxChunksPerBatch)
                    {
                        var batchChunks = chunks.ToList();
                        chunks.Clear();
                        return batchChunks;
                    }
                }
            }
            else
            {
                var allChunks = await chunkingStrategy.ChunkAsync(parsedContent, chunkingOptions, cancellationToken);
                chunks.AddRange(allChunks);
            }

            context.RecordSuccess(crawlResult.Url, chunks.Count);
            return chunks;
        }
        catch (Exception ex)
        {
            context.RecordError(new CrawlError { Message = ex.Message, ErrorType = ex.GetType().Name });
            return Enumerable.Empty<WebContentChunk>();
        }
    }
}

public class StreamingProcessingContext : IDisposable
{
    private readonly StreamingConfiguration _config;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly MemoryPressureController _memoryController;
    private readonly StreamingMetrics _metrics;
    private readonly Stopwatch _stopwatch;

    public IHttpClientFactory HttpClientFactory => _httpClientFactory;

    public StreamingProcessingContext(StreamingConfiguration config)
    {
        _config = config;
        _httpClientFactory = new DefaultHttpClientFactory();
        _memoryController = new MemoryPressureController(config.MemoryOptions);
        _metrics = new StreamingMetrics();
        _stopwatch = Stopwatch.StartNew();
    }

    public async Task CheckMemoryPressureAsync()
    {
        if (await _memoryController.IsMemoryPressureHighAsync())
        {
            GC.Collect(0, GCCollectionMode.Optimized);
            await Task.Delay(50); // 가비지 컬렉션 완료 대기
        }
    }

    public async Task<bool> ShouldApplyBackpressureAsync()
    {
        var memoryPressure = await _memoryController.GetMemoryPressureRatioAsync();
        var cpuUsage = await GetCpuUsageAsync();

        // 메모리 압박 > 80% 또는 CPU 사용률 > 90% 시 백프레셔
        return memoryPressure > 0.8 || cpuUsage > 90.0;
    }

    public void RecordSuccess(string url, int chunkCount)
    {
        _metrics.RecordSuccess(url, chunkCount);
    }

    public void RecordError(CrawlError error)
    {
        _metrics.RecordError(error);
    }

    public ProcessingProgress GetCurrentProgress()
    {
        return new ProcessingProgress
        {
            PagesProcessed = _metrics.ProcessedPages,
            ChunksGenerated = _metrics.GeneratedChunks,
            ElapsedTime = _stopwatch.Elapsed,
            ErrorCount = _metrics.ErrorCount,
            ThroughputPagesPerMinute = CalculateThroughput()
        };
    }

    private double CalculateThroughput()
    {
        var elapsedMinutes = _stopwatch.Elapsed.TotalMinutes;
        return elapsedMinutes > 0 ? _metrics.ProcessedPages / elapsedMinutes : 0;
    }

    public void Dispose()
    {
        _stopwatch?.Stop();
        _httpClientFactory?.Dispose();
        GC.SuppressFinalize(this);
    }
}
```

## 🧠 지능형 LRU 캐시 시스템

### URL 해시 기반 자동 캐싱

```csharp
public class IntelligentLRUCache : IWebContentCache
{
    private readonly Dictionary<string, CacheEntry> _cache;
    private readonly LinkedList<string> _accessOrder;
    private readonly ReaderWriterLockSlim _lock;
    private readonly CacheConfiguration _config;
    private readonly ICacheEvictionStrategy _evictionStrategy;
    private readonly Timer _cleanupTimer;

    public IntelligentLRUCache(CacheConfiguration config)
    {
        _config = config;
        _cache = new Dictionary<string, CacheEntry>();
        _accessOrder = new LinkedList<string>();
        _lock = new ReaderWriterLockSlim();
        _evictionStrategy = new AdaptiveEvictionStrategy(config);

        // 정기 클리너 설정
        _cleanupTimer = new Timer(PerformMaintenance, null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
    }

    public async Task<T?> GetAsync<T>(string key) where T : class
    {
        var cacheKey = GenerateIntelligentKey(key, typeof(T));

        _lock.EnterUpgradeableReadLock();
        try
        {
            if (_cache.TryGetValue(cacheKey, out var entry))
            {
                // 만료 체크
                if (entry.IsExpired)
                {
                    _lock.EnterWriteLock();
                    try
                    {
                        _cache.Remove(cacheKey);
                        _accessOrder.Remove(entry.AccessNode);
                        return null;
                    }
                    finally
                    {
                        _lock.ExitWriteLock();
                    }
                }

                // LRU 순서 업데이트
                _lock.EnterWriteLock();
                try
                {
                    _accessOrder.Remove(entry.AccessNode);
                    entry.AccessNode = _accessOrder.AddFirst(cacheKey);
                    entry.LastAccessedAt = DateTime.UtcNow;
                    entry.AccessCount++;
                }
                finally
                {
                    _lock.ExitWriteLock();
                }

                return entry.Value as T;
            }

            return null;
        }
        finally
        {
            _lock.ExitUpgradeableReadLock();
        }
    }

    public async Task SetAsync<T>(string key, T value, CacheEntryOptions? options = null) where T : class
    {
        var cacheKey = GenerateIntelligentKey(key, typeof(T));
        var entryOptions = options ?? _config.DefaultOptions;

        _lock.EnterWriteLock();
        try
        {
            // 기존 엔트리 제거
            if (_cache.TryGetValue(cacheKey, out var existingEntry))
            {
                _accessOrder.Remove(existingEntry.AccessNode);
            }

            // 캐시 크기 관리
            await EnsureCacheCapacity();

            // 새 엔트리 추가
            var accessNode = _accessOrder.AddFirst(cacheKey);
            var newEntry = new CacheEntry
            {
                Key = cacheKey,
                Value = value,
                AccessNode = accessNode,
                CreatedAt = DateTime.UtcNow,
                LastAccessedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow + entryOptions.Duration,
                Priority = CalculatePriority(key, value),
                Size = EstimateSize(value)
            };

            _cache[cacheKey] = newEntry;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    private string GenerateIntelligentKey(string originalKey, Type valueType)
    {
        // URL 정규화 및 해시 생성
        var normalizedUrl = NormalizeUrl(originalKey);
        var typePrefix = GetTypePrefix(valueType);

        using var sha256 = SHA256.Create();
        var keyBytes = Encoding.UTF8.GetBytes($"{typePrefix}:{normalizedUrl}");
        var hash = sha256.ComputeHash(keyBytes);

        return $"{typePrefix}:{Convert.ToHexString(hash)[..16]}";
    }

    private string NormalizeUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return url;

        var normalized = new UriBuilder(uri)
        {
            Scheme = uri.Scheme.ToLowerInvariant(),
            Host = uri.Host.ToLowerInvariant(),
            Path = uri.AbsolutePath.TrimEnd('/'),
            Query = string.Empty, // 쿼리 매개변수 제거
            Fragment = string.Empty
        };

        return normalized.ToString();
    }

    private async Task EnsureCacheCapacity()
    {
        while (_cache.Count >= _config.MaxEntries)
        {
            await _evictionStrategy.EvictEntryAsync(_cache, _accessOrder);
        }

        // 메모리 기반 제거
        var currentMemoryUsage = CalculateMemoryUsage();
        while (currentMemoryUsage > _config.MaxMemoryUsage)
        {
            await _evictionStrategy.EvictByMemoryPressure(_cache, _accessOrder);
            currentMemoryUsage = CalculateMemoryUsage();
        }
    }

    private CachePriority CalculatePriority(string key, object value)
    {
        // URL 패턴 기반 우선순위
        if (key.Contains("/api/") || key.Contains("/assets/"))
            return CachePriority.Low;

        if (key.Contains("/docs/") || key.Contains("/help/"))
            return CachePriority.High;

        // 값 크기 기반 우선순위
        var size = EstimateSize(value);
        if (size > 1024 * 1024) // 1MB 이상
            return CachePriority.Low;

        return CachePriority.Medium;
    }

    private void PerformMaintenance(object? state)
    {
        _lock.EnterWriteLock();
        try
        {
            var expiredKeys = _cache
                .Where(kvp => kvp.Value.IsExpired)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in expiredKeys)
            {
                if (_cache.TryGetValue(key, out var entry))
                {
                    _cache.Remove(key);
                    _accessOrder.Remove(entry.AccessNode);
                }
            }

            // 통계 업데이트
            UpdateCacheStatistics();
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }
}

public class CacheEntry
{
    public string Key { get; set; } = string.Empty;
    public object? Value { get; set; }
    public LinkedListNode<string> AccessNode { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime LastAccessedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public int AccessCount { get; set; }
    public CachePriority Priority { get; set; }
    public long Size { get; set; }

    public bool IsExpired => DateTime.UtcNow > ExpiresAt;
    public TimeSpan Age => DateTime.UtcNow - CreatedAt;
    public TimeSpan TimeSinceAccess => DateTime.UtcNow - LastAccessedAt;
}

public enum CachePriority
{
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4
}
```

### 적응형 캐시 제거 전략

```csharp
public class AdaptiveEvictionStrategy : ICacheEvictionStrategy
{
    private readonly CacheConfiguration _config;
    private readonly CacheStatistics _statistics;

    public async Task EvictEntryAsync(
        Dictionary<string, CacheEntry> cache,
        LinkedList<string> accessOrder)
    {
        var evictionMethod = DetermineOptimalEvictionMethod(cache);

        var candidateForEviction = evictionMethod switch
        {
            EvictionMethod.LRU => FindLRUCandidate(cache, accessOrder),
            EvictionMethod.LFU => FindLFUCandidate(cache),
            EvictionMethod.Priority => FindLowPriorityCandidate(cache),
            EvictionMethod.Size => FindLargestCandidate(cache),
            EvictionMethod.TTL => FindExpiringSoonCandidate(cache),
            _ => FindLRUCandidate(cache, accessOrder)
        };

        if (candidateForEviction != null)
        {
            cache.Remove(candidateForEviction.Key);
            accessOrder.Remove(candidateForEviction.AccessNode);
            _statistics.RecordEviction(candidateForEviction, evictionMethod);
        }
    }

    private EvictionMethod DetermineOptimalEvictionMethod(Dictionary<string, CacheEntry> cache)
    {
        var cacheAnalysis = AnalyzeCacheState(cache);

        // 메모리 압박이 높으면 큰 객체부터 제거
        if (cacheAnalysis.MemoryPressure > 0.9)
            return EvictionMethod.Size;

        // 만료 임박 객체가 많으면 TTL 기반
        if (cacheAnalysis.ExpiringSoonRatio > 0.3)
            return EvictionMethod.TTL;

        // 우선순위가 분명하게 나뉘어져 있으면 우선순위 기반
        if (cacheAnalysis.HasClearPriorityDistribution)
            return EvictionMethod.Priority;

        // 접근 빈도 차이가 크면 LFU
        if (cacheAnalysis.AccessFrequencyVariance > 10.0)
            return EvictionMethod.LFU;

        // 기본값: LRU
        return EvictionMethod.LRU;
    }

    private CacheEntry? FindLRUCandidate(
        Dictionary<string, CacheEntry> cache,
        LinkedList<string> accessOrder)
    {
        // 가장 오래된 접근 항목 (링크드 리스트의 마지막)
        var lastKey = accessOrder.Last?.Value;
        return lastKey != null && cache.TryGetValue(lastKey, out var entry) ? entry : null;
    }

    private CacheEntry? FindLFUCandidate(Dictionary<string, CacheEntry> cache)
    {
        // 가장 적게 접근된 항목
        return cache.Values
            .Where(entry => entry.Priority != CachePriority.Critical)
            .OrderBy(entry => entry.AccessCount)
            .ThenBy(entry => entry.LastAccessedAt)
            .FirstOrDefault();
    }

    private CacheEntry? FindLowPriorityCandidate(Dictionary<string, CacheEntry> cache)
    {
        // 우선순위가 낮은 항목
        return cache.Values
            .Where(entry => entry.Priority == CachePriority.Low)
            .OrderBy(entry => entry.LastAccessedAt)
            .FirstOrDefault()
            ?? cache.Values
                .Where(entry => entry.Priority == CachePriority.Medium)
                .OrderBy(entry => entry.LastAccessedAt)
                .FirstOrDefault();
    }

    private CacheEntry? FindLargestCandidate(Dictionary<string, CacheEntry> cache)
    {
        // 가장 큰 크기의 항목 (Critical 제외)
        return cache.Values
            .Where(entry => entry.Priority != CachePriority.Critical)
            .OrderByDescending(entry => entry.Size)
            .FirstOrDefault();
    }

    private CacheEntry? FindExpiringSoonCandidate(Dictionary<string, CacheEntry> cache)
    {
        var now = DateTime.UtcNow;
        var nearExpiryThreshold = TimeSpan.FromMinutes(5);

        // 곧 만료될 항목
        return cache.Values
            .Where(entry => entry.ExpiresAt - now <= nearExpiryThreshold)
            .OrderBy(entry => entry.ExpiresAt)
            .FirstOrDefault();
    }
}
```

## 💾 메모리 백프레셔 제어

### 지능형 메모리 관리

```csharp
public class AdvancedMemoryPressureController
{
    private readonly MemoryPressureConfiguration _config;
    private readonly IMemoryMonitor _memoryMonitor;
    private readonly IGarbageCollectionStrategy _gcStrategy;
    private readonly Timer _monitoringTimer;
    private volatile MemoryPressureLevel _currentLevel;

    public AdvancedMemoryPressureController(MemoryPressureConfiguration config)
    {
        _config = config;
        _memoryMonitor = new SystemMemoryMonitor();
        _gcStrategy = new AdaptiveGCStrategy(config.GCOptions);
        _currentLevel = MemoryPressureLevel.Normal;

        _monitoringTimer = new Timer(MonitorMemoryPressure, null, TimeSpan.Zero, TimeSpan.FromSeconds(1));
    }

    public async Task<MemoryPressureDecision> EvaluateMemoryPressureAsync()
    {
        var memoryStatus = await _memoryMonitor.GetMemoryStatusAsync();
        var newLevel = CalculateMemoryPressureLevel(memoryStatus);

        if (newLevel != _currentLevel)
        {
            var previousLevel = _currentLevel;
            _currentLevel = newLevel;
            await HandleMemoryPressureLevelChange(previousLevel, newLevel, memoryStatus);
        }

        return new MemoryPressureDecision
        {
            CurrentLevel = _currentLevel,
            ShouldThrottle = ShouldThrottleOperations(_currentLevel),
            RecommendedActions = GetRecommendedActions(_currentLevel),
            ThrottleDelay = GetThrottleDelay(_currentLevel)
        };
    }

    private MemoryPressureLevel CalculateMemoryPressureLevel(MemoryStatus status)
    {
        var totalMemory = status.TotalPhysicalMemory;
        var availableMemory = status.AvailablePhysicalMemory;
        var usedMemory = totalMemory - availableMemory;
        var memoryUsageRatio = (double)usedMemory / totalMemory;

        var processMemory = status.ProcessWorkingSet;
        var processMemoryRatio = (double)processMemory / totalMemory;

        // 멀티팩터 평가
        var pressureScore = CalculatePressureScore(memoryUsageRatio, processMemoryRatio, status);

        return pressureScore switch
        {
            < 0.3 => MemoryPressureLevel.Low,
            < 0.6 => MemoryPressureLevel.Normal,
            < 0.8 => MemoryPressureLevel.Medium,
            < 0.95 => MemoryPressureLevel.High,
            _ => MemoryPressureLevel.Critical
        };
    }

    private double CalculatePressureScore(
        double systemMemoryRatio,
        double processMemoryRatio,
        MemoryStatus status)
    {
        var baseScore = systemMemoryRatio * 0.6 + processMemoryRatio * 0.4;

        // GC 압박 가중치
        var gcPressure = (double)status.Gen2Collections / Math.Max(1, status.Gen0Collections);
        baseScore += gcPressure * 0.1;

        // 페이징 파일 사용량 가중치
        if (status.PageFileUsage > 0)
        {
            var pageFileRatio = (double)status.PageFileUsage / status.TotalPageFile;
            baseScore += pageFileRatio * 0.2;
        }

        // 메모리 단편화 가중치 (대형 객체 힙)
        var lohFragmentation = (double)status.LargeObjectHeapSize / Math.Max(1, status.TotalManagedMemory);
        baseScore += lohFragmentation * 0.1;

        return Math.Min(1.0, baseScore);
    }

    private async Task HandleMemoryPressureLevelChange(
        MemoryPressureLevel previousLevel,
        MemoryPressureLevel newLevel,
        MemoryStatus memoryStatus)
    {
        await _gcStrategy.HandleMemoryPressureLevelChange(previousLevel, newLevel, memoryStatus);

        // 레벨별 특화 처리
        switch (newLevel)
        {
            case MemoryPressureLevel.High:
                await OnHighMemoryPressure(memoryStatus);
                break;

            case MemoryPressureLevel.Critical:
                await OnCriticalMemoryPressure(memoryStatus);
                break;

            case MemoryPressureLevel.Normal when previousLevel >= MemoryPressureLevel.High:
                await OnMemoryPressureRecovered();
                break;
        }
    }

    private async Task OnHighMemoryPressure(MemoryStatus status)
    {
        // 1. 캐시 정리
        await RequestCacheClearance(0.3); // 30% 캐시 정리

        // 2. 대형 객체 정리
        GC.Collect(2, GCCollectionMode.Forced);
        GC.WaitForPendingFinalizers();

        // 3. 임시 버퍼 정리
        await ClearTemporaryBuffers();
    }

    private async Task OnCriticalMemoryPressure(MemoryStatus status)
    {
        // 긴급 메모리 해제
        await RequestCacheClearance(0.7); // 70% 캐시 정리

        // 강제 가비지 컬렉션 (모든 세대)
        for (int i = 0; i < 3; i++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            await Task.Delay(100);
        }

        // 대형 객체 힙 압축
        GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
        GC.Collect();

        // 작업 일시 중단 요청
        await RequestWorkloadReduction();
    }

    public async Task<bool> ShouldThrottleAsync(WorkItemType workItemType)
    {
        var decision = await EvaluateMemoryPressureAsync();

        if (!decision.ShouldThrottle)
            return false;

        // 작업 유형별 차별화된 스로틀링
        var throttlePriority = GetWorkItemThrottlePriority(workItemType);
        var levelThreshold = GetThrottleThreshold(_currentLevel);

        return throttlePriority <= levelThreshold;
    }

    private int GetWorkItemThrottlePriority(WorkItemType workItemType)
    {
        return workItemType switch
        {
            WorkItemType.ImageProcessing => 1, // 가장 먼저 스로틀링
            WorkItemType.WebCrawl => 2,
            WorkItemType.ContentExtraction => 3,
            WorkItemType.Parsing => 4,
            WorkItemType.Chunking => 5, // 마지막으로 스로틀링
            _ => 3
        };
    }

    private int GetThrottleThreshold(MemoryPressureLevel level)
    {
        return level switch
        {
            MemoryPressureLevel.Medium => 1, // 이미지 처리만 스로틀링
            MemoryPressureLevel.High => 3,   // 크롤링까지 스로틀링
            MemoryPressureLevel.Critical => 5, // 모든 작업 스로틀링
            _ => 0 // 스로틀링 안함
        };
    }
}

public enum MemoryPressureLevel
{
    Low,
    Normal,
    Medium,
    High,
    Critical
}

public class MemoryPressureDecision
{
    public MemoryPressureLevel CurrentLevel { get; set; }
    public bool ShouldThrottle { get; set; }
    public List<MemoryAction> RecommendedActions { get; set; } = new();
    public TimeSpan ThrottleDelay { get; set; }
}

public enum MemoryAction
{
    ClearCache,
    ForceGC,
    ReduceWorkers,
    PauseNonCriticalTasks,
    CompactLOH,
    FlushBuffers
}
```

## 📈 성능 모니터링 및 메트릭

### 실시간 성능 추적

```csharp
public class ComprehensivePerformanceMonitor : IPerformanceMonitor
{
    private readonly MetricsConfiguration _config;
    private readonly ConcurrentDictionary<string, PerformanceCounter> _counters;
    private readonly Timer _aggregationTimer;
    private readonly IMetricsExporter _exporter;

    public ComprehensivePerformanceMonitor(MetricsConfiguration config)
    {
        _config = config;
        _counters = new ConcurrentDictionary<string, PerformanceCounter>();
        _exporter = new PrometheusMetricsExporter();

        _aggregationTimer = new Timer(AggregateAndExportMetrics, null,
            config.AggregationInterval, config.AggregationInterval);
    }

    public void RecordProcessingTime(string operation, TimeSpan duration, bool success)
    {
        var key = $"processing_time_{operation}";
        var counter = _counters.GetOrAdd(key, _ => new PerformanceCounter(key));

        counter.RecordValue(duration.TotalMilliseconds);
        if (success)
        {
            counter.IncrementSuccessCount();
        }
        else
        {
            counter.IncrementErrorCount();
        }
    }

    public void RecordThroughput(string operation, int itemCount)
    {
        var key = $"throughput_{operation}";
        var counter = _counters.GetOrAdd(key, _ => new ThroughputCounter(key));
        (counter as ThroughputCounter)?.RecordItems(itemCount);
    }

    public void RecordMemoryUsage(long bytesUsed)
    {
        var key = "memory_usage";
        var counter = _counters.GetOrAdd(key, _ => new GaugeCounter(key));
        (counter as GaugeCounter)?.SetValue(bytesUsed);
    }

    public void RecordCacheMetrics(string operation, bool hit)
    {
        var hitKey = $"cache_hit_{operation}";
        var missKey = $"cache_miss_{operation}";

        if (hit)
        {
            _counters.GetOrAdd(hitKey, _ => new CounterMetric(hitKey)).Increment();
        }
        else
        {
            _counters.GetOrAdd(missKey, _ => new CounterMetric(missKey)).Increment();
        }
    }

    public async Task<PerformanceSnapshot> GetCurrentSnapshotAsync()
    {
        var snapshot = new PerformanceSnapshot
        {
            Timestamp = DateTime.UtcNow,
            Counters = new Dictionary<string, object>()
        };

        // 처리 시간 메트릭
        var processingTimeCounters = _counters.Where(kvp => kvp.Key.StartsWith("processing_time_"));
        foreach (var kvp in processingTimeCounters)
        {
            var counter = kvp.Value;
            snapshot.Counters[kvp.Key] = new
            {
                Average = counter.Average,
                Min = counter.Min,
                Max = counter.Max,
                Count = counter.Count,
                SuccessRate = counter.SuccessRate
            };
        }

        // 처리량 메트릭
        var throughputCounters = _counters.Where(kvp => kvp.Key.StartsWith("throughput_"));
        foreach (var kvp in throughputCounters)
        {
            var counter = kvp.Value as ThroughputCounter;
            if (counter != null)
            {
                snapshot.Counters[kvp.Key] = new
                {
                    ItemsPerSecond = counter.GetItemsPerSecond(),
                    TotalItems = counter.TotalItems
                };
            }
        }

        // 시스템 메트릭
        snapshot.SystemMetrics = await CollectSystemMetrics();

        return snapshot;
    }

    private async Task<SystemMetrics> CollectSystemMetrics()
    {
        var process = Process.GetCurrentProcess();

        return new SystemMetrics
        {
            CpuUsagePercent = await GetCpuUsageAsync(),
            MemoryUsageMB = process.WorkingSet64 / (1024 * 1024),
            ThreadCount = process.Threads.Count,
            HandleCount = process.HandleCount,
            GCGen0Collections = GC.CollectionCount(0),
            GCGen1Collections = GC.CollectionCount(1),
            GCGen2Collections = GC.CollectionCount(2),
            TotalManagedMemoryMB = GC.GetTotalMemory(false) / (1024 * 1024)
        };
    }

    private void AggregateAndExportMetrics(object? state)
    {
        Task.Run(async () =>
        {
            try
            {
                var snapshot = await GetCurrentSnapshotAsync();
                await _exporter.ExportAsync(snapshot);

                // 오래된 카운터 정리
                await CleanupOldCounters();
            }
            catch (Exception ex)
            {
                // 로깅 처리
                Console.WriteLine($"Metrics aggregation failed: {ex.Message}");
            }
        });
    }

    public async Task<PerformanceOptimizationRecommendations> AnalyzePerformanceAsync()
    {
        var snapshot = await GetCurrentSnapshotAsync();
        var recommendations = new PerformanceOptimizationRecommendations();

        // 성능 분석 및 권장사항 생성
        await AnalyzeThroughput(snapshot, recommendations);
        await AnalyzeMemoryUsage(snapshot, recommendations);
        await AnalyzeCacheEfficiency(snapshot, recommendations);
        await AnalyzeResourceUtilization(snapshot, recommendations);

        return recommendations;
    }

    private async Task AnalyzeThroughput(
        PerformanceSnapshot snapshot,
        PerformanceOptimizationRecommendations recommendations)
    {
        var crawlingThroughput = GetThroughputValue(snapshot, "throughput_WebCrawl");
        var targetThroughput = 100.0 / 60.0; // 100페이지/분 = 1.67페이지/초

        if (crawlingThroughput < targetThroughput * 0.8) // 80% 미만이면 권장사항 제시
        {
            recommendations.Add(OptimizationCategory.Performance,
                "크롤링 처리량이 목표치를 하회합니다.",
                new[]
                {
                    "병렬 워커 수 증가 검토",
                    "네트워크 요청 타임아웃 최적화",
                    "캐시 히트율 개선"
                });
        }
    }

    private async Task AnalyzeMemoryUsage(
        PerformanceSnapshot snapshot,
        PerformanceOptimizationRecommendations recommendations)
    {
        var memoryUsageMB = snapshot.SystemMetrics.MemoryUsageMB;
        var gen2Collections = snapshot.SystemMetrics.GCGen2Collections;

        if (memoryUsageMB > 1024) // 1GB 이상 사용
        {
            recommendations.Add(OptimizationCategory.Memory,
                "메모리 사용량이 높습니다.",
                new[]
                {
                    "스트리밍 처리 활용도 증가",
                    "캐시 크기 조정",
                    "대형 객체 생성 최소화"
                });
        }

        if (gen2Collections > 10) // Gen2 GC가 빈번함
        {
            recommendations.Add(OptimizationCategory.Memory,
                "대형 객체 힙 압박이 감지됩니다.",
                new[]
                {
                    "대형 객체 재사용 패턴 검토",
                    "메모리 스트리밍 최적화",
                    "객체 풀링 도입 검토"
                });
        }
    }
}

public class PerformanceOptimizationRecommendations
{
    public Dictionary<OptimizationCategory, List<OptimizationRecommendation>> Recommendations { get; set; } = new();

    public void Add(OptimizationCategory category, string issue, string[] suggestions)
    {
        if (!Recommendations.ContainsKey(category))
        {
            Recommendations[category] = new List<OptimizationRecommendation>();
        }

        Recommendations[category].Add(new OptimizationRecommendation
        {
            Issue = issue,
            Suggestions = suggestions.ToList(),
            Severity = CalculateSeverity(category, issue),
            EstimatedImpact = EstimateImpact(category, issue)
        });
    }
}

public enum OptimizationCategory
{
    Performance,
    Memory,
    Cache,
    Network,
    Threading,
    Quality
}
```

## 🔄 적응형 성능 조정

### 자동 성능 튜닝

```csharp
public class AdaptivePerformanceTuner : IPerformanceTuner
{
    private readonly IPerformanceMonitor _monitor;
    private readonly AdaptiveTuningConfiguration _config;
    private readonly Timer _tuningTimer;
    private PerformanceProfile _currentProfile;

    public AdaptivePerformanceTuner(
        IPerformanceMonitor monitor,
        AdaptiveTuningConfiguration config)
    {
        _monitor = monitor;
        _config = config;
        _currentProfile = PerformanceProfile.Balanced;

        _tuningTimer = new Timer(PerformTuningCycle, null,
            config.TuningInterval, config.TuningInterval);
    }

    private async void PerformTuningCycle(object? state)
    {
        try
        {
            var snapshot = await _monitor.GetCurrentSnapshotAsync();
            var analysis = await AnalyzePerformanceCharacteristics(snapshot);
            var newProfile = DetermineOptimalProfile(analysis);

            if (newProfile != _currentProfile)
            {
                await TransitionToProfile(newProfile, analysis);
                _currentProfile = newProfile;
            }

            await ApplyMicroAdjustments(analysis);
        }
        catch (Exception ex)
        {
            // 로깅
        }
    }

    private async Task<PerformanceAnalysis> AnalyzePerformanceCharacteristics(PerformanceSnapshot snapshot)
    {
        return new PerformanceAnalysis
        {
            ThroughputScore = CalculateThroughputScore(snapshot),
            MemoryEfficiencyScore = CalculateMemoryEfficiencyScore(snapshot),
            CacheEfficiencyScore = CalculateCacheEfficiencyScore(snapshot),
            ResourceUtilizationScore = CalculateResourceUtilizationScore(snapshot),
            QualityScore = CalculateQualityScore(snapshot),
            SystemLoad = await GetSystemLoad()
        };
    }

    private PerformanceProfile DetermineOptimalProfile(PerformanceAnalysis analysis)
    {
        // 메모리 압박이 심하면 메모리 최적화 프로필
        if (analysis.MemoryEfficiencyScore < 0.6)
            return PerformanceProfile.MemoryOptimized;

        // 처리량이 낮으면 처리량 최적화 프로필
        if (analysis.ThroughputScore < 0.7)
            return PerformanceProfile.ThroughputOptimized;

        // 품질이 낮으면 품질 우선 프로필
        if (analysis.QualityScore < 0.75)
            return PerformanceProfile.QualityFirst;

        // 시스템 부하가 높으면 보수적 프로필
        if (analysis.SystemLoad > 0.9)
            return PerformanceProfile.Conservative;

        return PerformanceProfile.Balanced;
    }

    private async Task TransitionToProfile(PerformanceProfile newProfile, PerformanceAnalysis analysis)
    {
        var transitionPlan = CreateTransitionPlan(_currentProfile, newProfile, analysis);

        foreach (var adjustment in transitionPlan.Adjustments)
        {
            await ApplyAdjustment(adjustment);
            await Task.Delay(adjustment.DelayAfterApplication);
        }
    }

    private async Task ApplyMicroAdjustments(PerformanceAnalysis analysis)
    {
        var adjustments = new List<PerformanceAdjustment>();

        // 처리량 기반 조정
        if (analysis.ThroughputScore < 0.8 && analysis.MemoryEfficiencyScore > 0.7)
        {
            adjustments.Add(new PerformanceAdjustment
            {
                Type = AdjustmentType.IncreaseParallelism,
                Value = Math.Min(Environment.ProcessorCount * 2, GetCurrentParallelism() + 1)
            });
        }

        // 메모리 기반 조정
        if (analysis.MemoryEfficiencyScore < 0.7)
        {
            adjustments.Add(new PerformanceAdjustment
            {
                Type = AdjustmentType.ReduceCacheSize,
                Value = Math.Max(100, GetCurrentCacheSize() * 0.9)
            });
        }

        // 캐시 효율성 기반 조정
        if (analysis.CacheEfficiencyScore < 0.6)
        {
            adjustments.Add(new PerformanceAdjustment
            {
                Type = AdjustmentType.AdjustCacheExpiration,
                Value = TimeSpan.FromMinutes(Math.Max(5, GetCurrentCacheExpiration().TotalMinutes * 1.2))
            });
        }

        foreach (var adjustment in adjustments)
        {
            await ApplyAdjustment(adjustment);
        }
    }
}

public enum PerformanceProfile
{
    Conservative,      // 안정성 우선, 낮은 리소스 사용
    MemoryOptimized,   // 메모리 효율성 최우선
    ThroughputOptimized, // 처리량 최대화
    QualityFirst,      // 품질 우선, 성능 일부 희생
    Balanced           // 균형잡힌 설정
}

public class PerformanceAdjustment
{
    public AdjustmentType Type { get; set; }
    public object Value { get; set; } = null!;
    public TimeSpan DelayAfterApplication { get; set; } = TimeSpan.FromSeconds(5);
    public string Reason { get; set; } = string.Empty;
}

public enum AdjustmentType
{
    IncreaseParallelism,
    DecreaseParallelism,
    IncreaseCacheSize,
    ReduceCacheSize,
    AdjustCacheExpiration,
    ModifyChunkingStrategy,
    AdjustMemoryThreshold,
    ChangeQualityThreshold
}
```

---

이 성능 설계는 연구 문서에서 제시한 **100페이지/분 크롤링 성능**과 **84% 메모리 절감** 등의 구체적인 성능 목표를 달성하기 위한 종합적인 최적화 전략을 제공합니다. 특히 동적 확장성과 지능형 리소스 관리를 통해 다양한 환경에서 최적의 성능을 발휘할 수 있도록 설계되었습니다.