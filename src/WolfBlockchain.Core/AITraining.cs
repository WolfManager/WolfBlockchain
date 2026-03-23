namespace WolfBlockchain.Core;

/// <summary>
/// Status-ul unui job de antrenament AI
/// </summary>
public enum AITrainingStatus
{
    /// <summary>In asteptare sa inceapa</summary>
    Queued = 0,
    
    /// <summary>In curs de execurare</summary>
    Running = 1,
    
    /// <summary>Completat cu succes</summary>
    Completed = 2,
    
    /// <summary>Eșuat</summary>
    Failed = 3,
    
    /// <summary>Anulat</summary>
    Cancelled = 4,
    
    /// <summary>Pausat</summary>
    Paused = 5
}

/// <summary>
/// Tipurile de modele AI
/// </summary>
public enum AIModelType
{
    /// <summary>Neural Network</summary>
    NeuralNetwork = 0,
    
    /// <summary>Deep Learning</summary>
    DeepLearning = 1,
    
    /// <summary>Reinforcement Learning</summary>
    ReinforcementLearning = 2,
    
    /// <summary>Natural Language Processing</summary>
    NLP = 3,
    
    /// <summary>Computer Vision</summary>
    ComputerVision = 4,
    
    /// <summary>Custom Model</summary>
    Custom = 5
}

/// <summary>
/// Model de antrenament AI
/// </summary>
public class AITrainingModel
{
    /// <summary>ID unic al modelului</summary>
    public string ModelId { get; set; }
    
    /// <summary>Nume model</summary>
    public string Name { get; set; }
    
    /// <summary>Descriere model</summary>
    public string Description { get; set; }
    
    /// <summary>Tipul de model</summary>
    public AIModelType Type { get; set; }
    
    /// <summary>Adresa proprietarului</summary>
    public string OwnerAddress { get; set; }
    
    /// <summary>Versiune model</summary>
    public string Version { get; set; }
    
    /// <summary>Data crearii</summary>
    public DateTime CreatedAt { get; set; }
    
    /// <summary>Data ultimei actualizari</summary>
    public DateTime UpdatedAt { get; set; }
    
    /// <summary>Status model</summary>
    public string Status { get; set; }
    
    /// <summary>Acuratete (0-100)</summary>
    public decimal Accuracy { get; set; }
    
    /// <summary>Loss value</summary>
    public decimal Loss { get; set; }
    
    /// <summary>Numarul de parametri</summary>
    public long ParameterCount { get; set; }
    
    /// <summary>Hash-ul modelului</summary>
    public string ModelHash { get; set; }
    
    /// <summary>Path-ul de stocare</summary>
    public string StoragePath { get; set; }
    
    /// <summary>Metadate</summary>
    public Dictionary<string, string> Metadata { get; set; }

    public AITrainingModel(string name, string description, AIModelType type, string ownerAddress)
    {
        ModelId = Guid.NewGuid().ToString();
        Name = name;
        Description = description;
        Type = type;
        OwnerAddress = ownerAddress;
        Version = "1.0.0";
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        Status = "Initialized";
        Accuracy = 0;
        Loss = 0;
        ParameterCount = 0;
        ModelHash = "";
        StoragePath = $"models/{ModelId}";
        Metadata = new Dictionary<string, string>();
    }

    /// <summary>Valideaza modelul</summary>
    public bool IsValid()
    {
        return !string.IsNullOrEmpty(Name) && !string.IsNullOrEmpty(OwnerAddress);
    }
}

/// <summary>
/// Job de antrenament AI
/// </summary>
public class AITrainingJob
{
    /// <summary>ID unic al jobului</summary>
    public string JobId { get; set; }
    
    /// <summary>ID modelului asociat</summary>
    public string ModelId { get; set; }
    
    /// <summary>Adresa utilizatorului care a lansat jobul</summary>
    public string UserAddress { get; set; }
    
    /// <summary>Status job</summary>
    public AITrainingStatus Status { get; set; }
    
    /// <summary>Procent completare (0-100)</summary>
    public int ProgressPercent { get; set; }
    
    /// <summary>Data inceput</summary>
    public DateTime StartedAt { get; set; }
    
    /// <summary>Data finish</summary>
    public DateTime? CompletedAt { get; set; }
    
    /// <summary>Durata in secunde</summary>
    public int DurationSeconds { get; set; }
    
    /// <summary>Numarul de epochs</summary>
    public int Epochs { get; set; }
    
    /// <summary>Batch size</summary>
    public int BatchSize { get; set; }
    
    /// <summary>Learning rate</summary>
    public decimal LearningRate { get; set; }
    
    /// <summary>Numarul de dataset samples</summary>
    public int DatasetSize { get; set; }
    
    /// <summary>Mesaj de eroare (daca e cazul)</summary>
    public string? ErrorMessage { get; set; }
    
    /// <summary>Rezultate antrenament</summary>
    public Dictionary<string, object> Results { get; set; }
    
    /// <summary>Taxa pentru antrenament (in WOLF)</summary>
    public decimal TrainingFee { get; set; }

    public AITrainingJob(string modelId, string userAddress, int epochs, int batchSize, decimal learningRate, int datasetSize)
    {
        JobId = Guid.NewGuid().ToString();
        ModelId = modelId;
        UserAddress = userAddress;
        Status = AITrainingStatus.Queued;
        ProgressPercent = 0;
        StartedAt = DateTime.UtcNow;
        CompletedAt = null;
        DurationSeconds = 0;
        Epochs = epochs;
        BatchSize = batchSize;
        LearningRate = learningRate;
        DatasetSize = datasetSize;
        ErrorMessage = null;
        Results = new Dictionary<string, object>();
        TrainingFee = 0;
    }

    /// <summary>Marchez jobul ca running</summary>
    public void Start()
    {
        Status = AITrainingStatus.Running;
        StartedAt = DateTime.UtcNow;
    }

    /// <summary>Marchez jobul ca completat</summary>
    public void Complete(decimal accuracy, decimal loss)
    {
        Status = AITrainingStatus.Completed;
        CompletedAt = DateTime.UtcNow;
        DurationSeconds = (int)(CompletedAt.Value - StartedAt).TotalSeconds;
        ProgressPercent = 100;
        Results["Accuracy"] = accuracy;
        Results["Loss"] = loss;
        Results["CompletionTime"] = DurationSeconds;
    }

    /// <summary>Marchez jobul ca eșuat</summary>
    public void Fail(string errorMessage)
    {
        Status = AITrainingStatus.Failed;
        ErrorMessage = errorMessage;
        CompletedAt = DateTime.UtcNow;
    }

    /// <summary>Anuleaza jobul</summary>
    public void Cancel()
    {
        Status = AITrainingStatus.Cancelled;
        CompletedAt = DateTime.UtcNow;
    }

    /// <summary>Pauzează jobul</summary>
    public void Pause()
    {
        Status = AITrainingStatus.Paused;
    }
}

/// <summary>
/// Dataset pentru antrenament
/// </summary>
public class AITrainingDataset
{
    /// <summary>ID unic al dataset-ului</summary>
    public string DatasetId { get; set; }
    
    /// <summary>Nume dataset</summary>
    public string Name { get; set; }
    
    /// <summary>Descriere</summary>
    public string Description { get; set; }
    
    /// <summary>Adresa proprietarului</summary>
    public string OwnerAddress { get; set; }
    
    /// <summary>Numarul de samples</summary>
    public int SampleCount { get; set; }
    
    /// <summary>Numarul de features</summary>
    public int FeatureCount { get; set; }
    
    /// <summary>Dimensiunea dataset-ului in MB</summary>
    public decimal SizeMB { get; set; }
    
    /// <summary>Format date (CSV, JSON, Binary, etc)</summary>
    public string Format { get; set; }
    
    /// <summary>Path stocare</summary>
    public string StoragePath { get; set; }
    
    /// <summary>Data crearii</summary>
    public DateTime CreatedAt { get; set; }
    
    /// <summary>Hash dataset</summary>
    public string DatasetHash { get; set; }

    public AITrainingDataset(string name, string description, string ownerAddress, int sampleCount, int featureCount)
    {
        DatasetId = Guid.NewGuid().ToString();
        Name = name;
        Description = description;
        OwnerAddress = ownerAddress;
        SampleCount = sampleCount;
        FeatureCount = featureCount;
        SizeMB = 0;
        Format = "CSV";
        StoragePath = $"datasets/{DatasetId}";
        CreatedAt = DateTime.UtcNow;
        DatasetHash = "";
    }
}

/// <summary>
/// Metrika antrenament
/// </summary>
public class AITrainingMetric
{
    public string MetricId { get; set; }
    public string JobId { get; set; }
    public int Epoch { get; set; }
    public decimal TrainingLoss { get; set; }
    public decimal ValidationLoss { get; set; }
    public decimal TrainingAccuracy { get; set; }
    public decimal ValidationAccuracy { get; set; }
    public DateTime Timestamp { get; set; }

    public AITrainingMetric(string jobId, int epoch)
    {
        MetricId = Guid.NewGuid().ToString();
        JobId = jobId;
        Epoch = epoch;
        Timestamp = DateTime.UtcNow;
    }
}
