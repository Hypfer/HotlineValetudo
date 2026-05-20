namespace HotlineValetudo.Config;

public class TtsOptions
{
    public string ModelPath { get; set; } = "models/tts/vits-piper-en_US-lessac-medium/en_US-lessac-medium.onnx";
    public string TokensPath { get; set; } = "models/tts/vits-piper-en_US-lessac-medium/tokens.txt";
    public string DataDir { get; set; } = "models/tts/vits-piper-en_US-lessac-medium/espeak-ng-data";
    public float Speed { get; set; } = 1.0f;
    public int SpeakerId { get; set; } = 0;
    public int NumThreads { get; set; } = 1;
}