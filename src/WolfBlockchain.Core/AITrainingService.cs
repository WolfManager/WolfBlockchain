namespace WolfBlockchain.Core;

/// <summary>
/// Serviciu pentru gestionare antrenament AI
/// </summary>
public class AITrainingService
{
    private Dictionary<string, AITrainingModel> _models;
    private Dictionary<string, AITrainingJob> _jobs;
    private Dictionary<string, AITrainingDataset> _datasets;
    private List<AITrainingMetric> _metrics;
    private string _storageBasePath;
    private decimal _trainingCostPerEpoch = 0.1m; // Cost in WOLF per epoch

    public AITrainingService(string storageBasePath = "ai_models")
    {
        _models = new Dictionary<string, AITrainingModel>();
        _jobs = new Dictionary<string, AITrainingJob>();
        _datasets = new Dictionary<string, AITrainingDataset>();
        _metrics = new List<AITrainingMetric>();
        _storageBasePath = storageBasePath;

        if (!Directory.Exists(_storageBasePath))
            Directory.CreateDirectory(_storageBasePath);
    }

    /// <summary>Creeaza un nou model AI</summary>
    public AITrainingModel? CreateModel(string name, string description, AIModelType type, string ownerAddress)
    {
        var model = new AITrainingModel(name, description, type, ownerAddress);

        if (!model.IsValid())
            return null;

        _models[model.ModelId] = model;

        // Creeaza director pentru model
        var modelPath = Path.Combine(_storageBasePath, model.ModelId);
        Directory.CreateDirectory(modelPath);

        Console.WriteLine($"✅ AI Model created: {name} ({type})");
        return model;
    }

    /// <summary>Obtine model dupa ID</summary>
    public AITrainingModel? GetModel(string modelId)
    {
        return _models.ContainsKey(modelId) ? _models[modelId] : null;
    }

    /// <summary>Lista toti modelele unui utilizator</summary>
    public List<AITrainingModel> GetUserModels(string ownerAddress)
    {
        return _models.Values.Where(m => m.OwnerAddress == ownerAddress).ToList();
    }

    /// <summary>Creeaza dataset pentru antrenament</summary>
    public AITrainingDataset? CreateDataset(string name, string description, string ownerAddress, int sampleCount, int featureCount)
    {
        var dataset = new AITrainingDataset(name, description, ownerAddress, sampleCount, featureCount);
        _datasets[dataset.DatasetId] = dataset;

        var datasetPath = Path.Combine(_storageBasePath, "datasets", dataset.DatasetId);
        Directory.CreateDirectory(datasetPath);

        Console.WriteLine($"✅ Dataset created: {name} ({sampleCount} samples)");
        return dataset;
    }

    /// <summary>Obtine dataset dupa ID</summary>
    public AITrainingDataset? GetDataset(string datasetId)
    {
        return _datasets.ContainsKey(datasetId) ? _datasets[datasetId] : null;
    }

    /// <summary>Lanseaza un job de antrenament</summary>
    public AITrainingJob? StartTrainingJob(string modelId, string userAddress, string datasetId, 
        int epochs = 10, int batchSize = 32, decimal learningRate = 0.001m)
    {
        var model = GetModel(modelId);
        var dataset = GetDataset(datasetId);

        if (model == null || dataset == null)
        {
            Console.WriteLine("❌ Model or dataset not found");
            return null;
        }

        if (model.OwnerAddress != userAddress && dataset.OwnerAddress != userAddress)
        {
            Console.WriteLine("❌ Unauthorized: You don't own this model or dataset");
            return null;
        }

        var job = new AITrainingJob(modelId, userAddress, epochs, batchSize, learningRate, dataset.SampleCount);
        job.TrainingFee = epochs * _trainingCostPerEpoch;

        _jobs[job.JobId] = job;

        Console.WriteLine($"✅ Training job created: {job.JobId}");
        Console.WriteLine($"   Estimated cost: {job.TrainingFee} WOLF");

        return job;
    }

    /// <summary>Obtine job dupa ID</summary>
    public AITrainingJob? GetJob(string jobId)
    {
        return _jobs.ContainsKey(jobId) ? _jobs[jobId] : null;
    }

    /// <summary>Lista joburi ale unui utilizator</summary>
    public List<AITrainingJob> GetUserJobs(string userAddress)
    {
        return _jobs.Values.Where(j => j.UserAddress == userAddress).ToList();
    }

    /// <summary>Simuleaza progresul unui job</summary>
    public void UpdateJobProgress(string jobId, int progressPercent)
    {
        var job = GetJob(jobId);
        if (job == null) return;

        job.ProgressPercent = Math.Min(progressPercent, 100);

        if (progressPercent >= 100 && job.Status == AITrainingStatus.Running)
        {
            var accuracy = 85m + (decimal)(new Random().NextDouble() * 10); // 85-95% accuracy
            var loss = (decimal)(new Random().NextDouble() * 0.5); // Loss 0-0.5
            job.Complete(accuracy, loss);
            Console.WriteLine($"✅ Training job completed: {jobId}");
        }

        Console.WriteLine($"📊 Job {jobId}: {progressPercent}% complete");
    }

    /// <summary>Inregistreaza metrici pentru un job</summary>
    public void LogMetric(string jobId, int epoch, decimal trainingLoss, decimal validationLoss, 
        decimal trainingAccuracy, decimal validationAccuracy)
    {
        var metric = new AITrainingMetric(jobId, epoch)
        {
            TrainingLoss = trainingLoss,
            ValidationLoss = validationLoss,
            TrainingAccuracy = trainingAccuracy,
            ValidationAccuracy = validationAccuracy
        };

        _metrics.Add(metric);
    }

    /// <summary>Obtine metrici pentru un job</summary>
    public List<AITrainingMetric> GetJobMetrics(string jobId)
    {
        return _metrics.Where(m => m.JobId == jobId).ToList();
    }

    /// <summary>Anuleaza un job</summary>
    public bool CancelJob(string jobId)
    {
        var job = GetJob(jobId);
        if (job == null || job.Status == AITrainingStatus.Completed)
            return false;

        job.Cancel();
        Console.WriteLine($"🛑 Job cancelled: {jobId}");
        return true;
    }

    /// <summary>Pauzează un job</summary>
    public bool PauseJob(string jobId)
    {
        var job = GetJob(jobId);
        if (job == null || job.Status != AITrainingStatus.Running)
            return false;

        job.Pause();
        Console.WriteLine($"⏸️  Job paused: {jobId}");
        return true;
    }

    /// <summary>Reia un job pausat</summary>
    public bool ResumeJob(string jobId)
    {
        var job = GetJob(jobId);
        if (job == null || job.Status != AITrainingStatus.Paused)
            return false;

        job.Start();
        Console.WriteLine($"▶️  Job resumed: {jobId}");
        return true;
    }

    /// <summary>Obtine statistici de antrenament</summary>
    public Dictionary<string, object> GetTrainingStatistics()
    {
        var completedJobs = _jobs.Values.Count(j => j.Status == AITrainingStatus.Completed);
        var runningJobs = _jobs.Values.Count(j => j.Status == AITrainingStatus.Running);
        var failedJobs = _jobs.Values.Count(j => j.Status == AITrainingStatus.Failed);
        var totalCost = _jobs.Values.Sum(j => j.TrainingFee);
        var avgAccuracy = _jobs.Values
            .Where(j => j.Status == AITrainingStatus.Completed && j.Results.ContainsKey("Accuracy"))
            .Average(j => (decimal)j.Results["Accuracy"]);

        return new Dictionary<string, object>
        {
            { "TotalModels", _models.Count },
            { "TotalDatasets", _datasets.Count },
            { "TotalJobs", _jobs.Count },
            { "CompletedJobs", completedJobs },
            { "RunningJobs", runningJobs },
            { "FailedJobs", failedJobs },
            { "TotalTrainingCost", totalCost },
            { "AverageAccuracy", avgAccuracy },
            { "CostPerEpoch", _trainingCostPerEpoch }
        };
    }

    /// <summary>Salveaza model in storage</summary>
    public bool SaveModel(string modelId, byte[] modelData)
    {
        var model = GetModel(modelId);
        if (model == null)
            return false;

        try
        {
            var modelPath = Path.Combine(_storageBasePath, modelId, "model.bin");
            File.WriteAllBytes(modelPath, modelData);
            Console.WriteLine($"💾 Model saved: {modelId}");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error saving model: {ex.Message}");
            return false;
        }
    }

    /// <summary>Incarca model din storage</summary>
    public byte[]? LoadModel(string modelId)
    {
        var model = GetModel(modelId);
        if (model == null)
            return null;

        try
        {
            var modelPath = Path.Combine(_storageBasePath, modelId, "model.bin");
            if (File.Exists(modelPath))
                return File.ReadAllBytes(modelPath);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error loading model: {ex.Message}");
        }

        return null;
    }
}
