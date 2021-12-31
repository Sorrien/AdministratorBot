using AdministratorBot.Infrastructure;
using AdministratorBot.Logic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace AdministratorBot.Tests
{
    //Real tests under TODO
    public class ServerLogicTests
    {
        protected Mock<IConfiguration> ConfigurationMock;
        protected Mock<IProcessWrapper> ProcessLogicMock;
        protected Mock<IIPWrapper> IPLogicMock;
        protected Mock<ILogger<ServerLogic>> LoggerMock;

        protected ServerLogic ServerLogic;

        public virtual void Setup()
        {
            ConfigurationMock = new Mock<IConfiguration>();
            ProcessLogicMock = new Mock<IProcessWrapper>();
            IPLogicMock = new Mock<IIPWrapper>();
            LoggerMock = new Mock<ILogger<ServerLogic>>();
            ServerLogic = new ServerLogic(ConfigurationMock.Object, ProcessLogicMock.Object, IPLogicMock.Object, LoggerMock.Object);
        }
    }

    public class ServerStartTests : ServerLogicTests
    {
        [SetUp]
        public override void Setup()
        {
            base.Setup();

        }

        [Test]
        public void Start()
        {

        }
    }

    public class ServerGetServersTests : ServerLogicTests
    {
      //  [SetUp]
      //  public override void Setup()
      //  {
      //      base.Setup();
      //      var configuration = new ConfigurationBuilder().AddInMemoryCollection(new List<KeyValuePair<string, string>> { new KeyValuePair<string, string>("Servers",@"{
      //""Name"": ""Conan Exiles"",
      //""Address"": ""11.2.33.444"",
      //""Port"": ""24000"",
      //""HasLauncher"":  ""true"",
      //""Start"": ""C:\\ConanExiles\\ConanSandboxServer.exe"",
      //""StartArguments"": ""-log"",
      //""Update"": ""C:\\steamcmd\\steamcmd.exe"",
      //""UpdateArguments"": ""+login anonymous +force_install_dir \""C:\\GameServers\\ConanExiles\"" +app_update 443030 validate +quit""}") }).Build();
      //      ConfigurationMock.Setup(x => x.GetSection("Servers")).Returns(new ConfigurationSection(new ConfigurationRoot()) { Value = "" } )
      //  }

        [Test]
        public void GetServers()
        {

        }
    }
}