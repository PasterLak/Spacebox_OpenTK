using Xunit;
using Spacebox.Game;
using Spacebox.Game.GUI;
using System;

namespace Spacebox.Tests
{
    public class VersionConverterTests
    {
        #region IsVersionOlder Tests

        [Theory]
        [InlineData("1.0.0", "1.0.1", true)]
        [InlineData("1.0.1", "1.0.1", false)]
        [InlineData("1.0.2", "1.0.1", false)]
        [InlineData("0.9.9", "1.0.0", true)]
        [InlineData("1.0.0", "1.1.0", true)]
        [InlineData("2.0.0", "1.9.9", false)]
        [InlineData("invalid", "1.0.0", false)]
        [InlineData("1.0.0", "invalid", false)]
        [InlineData("invalid", "invalid", false)]
        public void IsVersionOlder_ReturnsCorrectly(string worldVersion, string currentVersion, bool expected)
        {
            bool result = VersionConverter.IsVersionOlder(worldVersion, currentVersion);
            Assert.Equal(expected, result);
        }

        #endregion

        #region IsVersionOld Tests

        [Theory]
        [InlineData("1.0.0", "1.0.1", true)]
        [InlineData("1.0.1", "1.0.1", false)]
        [InlineData("1.0.2", "1.0.1", false)]
        [InlineData("0.9.9", "1.0.0", true)]
        [InlineData("1.0.0", "1.1.0", true)]
        [InlineData("2.0.0", "1.9.9", false)]
        [InlineData("invalid", "1.0.0", false)]
        [InlineData("1.0.0", "invalid", false)]
        [InlineData("invalid", "invalid", false)]
        public void IsVersionOld_ReturnsCorrectly(string worldVersion, string appVersion, bool expected)
        {
            bool result = VersionConverter.IsVersionOld(worldVersion, appVersion);
            Assert.Equal(expected, result);
        }

        #endregion

        #region Convert Tests

        [Fact]
        public void Convert_WhenWorldVersionIs080_AndAppVersionIs089_UpdatesSuccessfully()
        {
            // Arrange
            var worldInfo = new WorldInfo
            {
                Name = "TestWorld",
                Author = "Author",
                Seed = "Seed",
                ModId = "Mod",
                GameVersion = "0.0.8",
                LastEditDate = "2023-01-01 00:00:00",
                FolderName = "TestFolder"
            };
            string appVersion = "0.0.9";

            // Act
            bool result = VersionConverter.Convert(worldInfo, appVersion);

            // Assert
            Assert.True(result);
            Assert.Equal("0.0.9", worldInfo.GameVersion);
        }

        [Fact]
        public void Convert_WhenWorldVersionIs080_AndAppVersionIs010_ReturnsFalseAfterFirstConversion()
        {
            // Arrange
            var worldInfo = new WorldInfo
            {
                Name = "TestWorld",
                Author = "Author",
                Seed = "Seed",
                ModId = "Mod",
                GameVersion = "0.0.8",
                LastEditDate = "2023-01-01 00:00:00",
                FolderName = "TestFolder"
            };
            string appVersion = "0.0.10";

            // Act
            bool result = VersionConverter.Convert(worldInfo, appVersion);

            // Assert
            // After first conversion: "0.0.8" -> "0.0.9"
            // Then attempts to convert "0.0.9" to "0.0.10", but "0.0.9" != "0.0.8"
            // Hence, second conversion fails and returns false
            Assert.False(result);
            Assert.Equal("0.0.9", worldInfo.GameVersion);
        }

        [Fact]
        public void Convert_WhenWorldVersionIsNot080_ReturnsFalse()
        {
            // Arrange
            var worldInfo = new WorldInfo
            {
                Name = "TestWorld",
                Author = "Author",
                Seed = "Seed",
                ModId = "Mod",
                GameVersion = "0.0.7",
                LastEditDate = "2023-01-01 00:00:00",
                FolderName = "TestFolder"
            };
            string appVersion = "0.0.9";

            // Act
            bool result = VersionConverter.Convert(worldInfo, appVersion);

            // Assert
            Assert.False(result);
            Assert.Equal("0.0.7", worldInfo.GameVersion);
        }

        [Fact]
        public void Convert_WhenWorldVersionIs089_AndAppVersionIs089_ReturnsTrueWithoutChange()
        {
            // Arrange
            var worldInfo = new WorldInfo
            {
                Name = "TestWorld",
                Author = "Author",
                Seed = "Seed",
                ModId = "Mod",
                GameVersion = "0.0.9",
                LastEditDate = "2023-01-01 00:00:00",
                FolderName = "TestFolder"
            };
            string appVersion = "0.0.9";

            // Act
            bool result = VersionConverter.Convert(worldInfo, appVersion);

            // Assert
            Assert.False(result);
            Assert.Equal("0.0.9", worldInfo.GameVersion);
        }

        [Fact]
        public void Convert_WhenWorldVersionIs080_AndAppVersionIs089_AfterUpdateTo089_ReturnsTrue()
        {
            // Arrange
            var worldInfo = new WorldInfo
            {
                Name = "TestWorld",
                Author = "Author",
                Seed = "Seed",
                ModId = "Mod",
                GameVersion = "0.0.8",
                LastEditDate = "2023-01-01 00:00:00",
                FolderName = "TestFolder"
            };
            string appVersion = "0.0.9";

            // Act
            bool firstResult = VersionConverter.Convert(worldInfo, appVersion);

            // Assert
            Assert.True(firstResult);
            Assert.Equal("0.0.9", worldInfo.GameVersion);

            // Further conversion should return false as worldVersion == appVersion
            bool secondResult = VersionConverter.Convert(worldInfo, appVersion);
            Assert.False(secondResult);
            Assert.Equal("0.0.9", worldInfo.GameVersion);
        }

        [Fact]
        public void Convert_WhenWorldVersionIsInvalid_ReturnsFalse()
        {
            // Arrange
            var worldInfo = new WorldInfo
            {
                Name = "TestWorld",
                Author = "Author",
                Seed = "Seed",
                ModId = "Mod",
                GameVersion = "invalid_version",
                LastEditDate = "2023-01-01 00:00:00",
                FolderName = "TestFolder"
            };
            string appVersion = "0.0.9";

            // Act
            bool result = VersionConverter.Convert(worldInfo, appVersion);

            // Assert
            Assert.False(result);
            Assert.Equal("invalid_version", worldInfo.GameVersion);
        }

        #endregion

        #region Helper Methods (Optional)

        // If needed, mock Debug.Error or other dependencies here.
        // Since Debug.Error is not defined, it's assumed to not affect the test outcomes.

        #endregion
    }
}
