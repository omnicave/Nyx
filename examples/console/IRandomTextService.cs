namespace Nyx.Examples.Console;

public interface IRandomTextService
{
    string GetRandomSentence();
    string GetRandomSentence(int minLength, int maxLength);
    string GetRandomSentence(int length);
}

public class RandomTextService : IRandomTextService
{
    public RandomTextService()
    {
        
    }

    public string GetRandomSentence() => LoremNETCore.Generate.Sentence(3, 20);
    public string GetRandomSentence(int minLength, int maxLength) => LoremNETCore.Generate.Sentence(minLength, maxLength);
    public string GetRandomSentence(int length) => LoremNETCore.Generate.Sentence(length);
}