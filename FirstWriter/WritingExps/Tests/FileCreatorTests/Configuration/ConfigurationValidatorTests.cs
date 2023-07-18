using FileCreator.Configuration;
using Moq;

namespace FileCreatorTests.Configuration;

public class ConfigurationValidatorTests
{
    [Theory]
    [InlineData("12345678", 12345678)]
    [InlineData("2kb", 2048)]
    [InlineData("3 MB", 3_145_728)]
    //bad: can depend on the local environment configuration - localization settings '.' vs ','
    [InlineData("1.4 Gb", 1_503_238_553)]
    [InlineData("1 Gb", 1_073_741_824)]
    [InlineData("2 mb 300kb", 2_097_152)]
    public void ConfigurationValidator_ShouldParseCorrectFileSize(string strValue, ulong size)
    {
        //In this case, this can be redundant.
        ConfigurationValidator validator = new ConfigurationValidator();
        Mock<IBaseConfiguration> baseConfMock = new Mock<IBaseConfiguration>();
        baseConfMock.Setup(bc => bc.FileSize).Returns(strValue);

        var conf = validator.ProvideConfiguration(baseConfMock.Object);

        Assert.Equal(size, conf.FileSize);
    }

    [Theory]
    [InlineData("mb2kb", 123)]
    [InlineData("a MB", 456)]
    [InlineData("3 Bg", 789)]
    public void ConfigurationValidator_ShouldUseDefaultValuesInsteadOfWrongInputParameters(string strValue, ulong size)
    {
        Mock<IRuntimeConfiguration> runtimeConfMock = new Mock<IRuntimeConfiguration>();
        runtimeConfMock.Setup(rc => rc.FileSize).Returns(size);
        runtimeConfMock.Setup(rc => rc.FilePath).Returns("");
        TestableConfigurationValidator validator = new TestableConfigurationValidator(runtimeConfMock);

        Mock<IBaseConfiguration> baseConfMock = new Mock<IBaseConfiguration>();
        baseConfMock.Setup(bc => bc.FileSize).Returns(strValue);
        baseConfMock.Setup(bc => bc.Encoding).Returns("UTF-8");
        baseConfMock.Setup(bc => bc.Delimiter).Returns(";");

        var conf = validator.ProvideConfiguration(baseConfMock.Object);

        Assert.Equal(size, conf.FileSize);
    }
    
    private class TestableConfigurationValidator : ConfigurationValidator
    {
        //Sometimes it can be useful, ability to add some additional behavior or something else.
        //But not in this case.
        public TestableConfigurationValidator(Mock<IRuntimeConfiguration> configMock) 
            : base(configMock.Object)
        {
        }
    }
}