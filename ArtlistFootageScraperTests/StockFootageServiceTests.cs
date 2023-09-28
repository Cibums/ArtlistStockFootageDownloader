using NSubstitute;
using Xunit;
using OpenQA.Selenium;
using Microsoft.Extensions.Logging;
using ArtlistFootageScraper.Services;
using OpenQA.Selenium.Support.UI;

namespace ArtlistFootageScraper.Tests
{
    public class StockFootageServiceTests
    {
        private readonly IWebDriver _mockDriver = Substitute.For<IWebDriver>();
        private readonly WebDriverWait _mockWait;
        private readonly ILogger<IStockFootageService> _mockLogger = Substitute.For<ILogger<IStockFootageService>>();
        private readonly IFileService _mockFileService = Substitute.For<IFileService>();

        public StockFootageServiceTests()
        {
            _mockWait = new WebDriverWait(_mockDriver, new System.TimeSpan(0, 0, 10));
        }

        [Fact]
        public void GenerateStockFootageFromKeywordsSynchronously_ReturnsExpectedFilePath_WhenSuccessful()
        {
            // Arrange
            var service = new StockFootageService(_mockDriver, _mockWait, _mockLogger, _mockFileService);
            var mockElement = Substitute.For<IWebElement>();

            _mockDriver.FindElement(Arg.Any<By>()).Returns(mockElement);
            mockElement.Displayed.Returns(true);
            _mockFileService.GetLatestDownloadedFile(Arg.Any<string>()).Returns("testpath.mp4");

            // Act
            var result = service.GenerateStockFootageFromKeywordsSynchronously(new[] { "keyword1", "keyword2" }, "someDirectory");

            // Assert
            Assert.Equal("testpath.mp4", result);
        }

        [Fact]
        public void SearchFootage_Navigates_To_Correct_URL()
        {
            // Arrange
            var service = new StockFootageService(_mockDriver, _mockWait, _mockLogger, _mockFileService);

            // Act
            service.SearchFootage(new[] { "keyword1", "keyword2" });

            // Assert
            _mockDriver.Received().Navigate().GoToUrl("https://artlist.io/stock-footage/search?terms=keyword1, keyword2");
        }

        [Fact]
        public void DownloadFootage_Returns_Previously_Downloaded_File_Path()
        {
            // Arrange
            var service = new StockFootageService(_mockDriver, _mockWait, _mockLogger, _mockFileService);
            var mockElement = Substitute.For<IWebElement>();
            _mockDriver.FindElement(Arg.Any<By>()).Returns(mockElement);
            mockElement.Displayed.Returns(true);
            _mockFileService.GetLatestDownloadedFile(Arg.Any<string>()).Returns("testpath.mp4");

            // Act
            var result = service.GenerateStockFootageFromKeywordsSynchronously(new[] { "keyword1" }, "someDirectory");

            // Assert
            Assert.Equal("testpath.mp4", result);
        }

        [Fact]
        public void DownloadFootage_Throws_TimeoutException_When_Download_Does_Not_Start()
        {
            // Arrange
            var service = new StockFootageService(_mockDriver, _mockWait, _mockLogger, _mockFileService);
            var mockElement = Substitute.For<IWebElement>();
            _mockDriver.FindElement(Arg.Any<By>()).Returns(mockElement);
            mockElement.Displayed.Returns(true);

            // Act and Assert
            Assert.Throws<TimeoutException>(() => service.GenerateStockFootageFromKeywordsSynchronously(new[] { "keyword1" }, "someDirectory"));
        }
    }
}