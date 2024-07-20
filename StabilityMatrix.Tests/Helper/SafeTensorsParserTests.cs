using StabilityMatrix.Avalonia.Helpers;

namespace StabilityMatrix.Tests.Helper;

[TestClass]
public class SafeTensorsParserTests
{
    [TestMethod]
    public void SafeTensorsParserTests_Parse()
    {
        var filePath =
            "D:\\StableDiffusion\\sd.webui\\webui\\models\\Lora\\CherrySchoolUniformV1-000009.safetensors";
        var result = SafeTensorsParser.ParseSafeTensorsMetadata(filePath);
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void SafeTensorsParserTests_GetSafeTensorsInfo()
    {
        var filePath =
            "D:\\StableDiffusion\\training\\img\\SaraBattleUniform\\model\\SaraBattleUniformV1.safetensors";
        var json = SafeTensorsParser.ParseSafeTensorsMetadata(filePath, 5);
        var result = SafeTensorsParser.GetSafeTensorsInfo(json.Values.First());
        Assert.IsNotNull(result);
    }
}
