using Xunit;
using Spacebox.Game;
using System;
using Spacebox.Game.Player;

namespace Spacebox.Tests
{
    public class StatsBarDataTests
    {
        #region Initialization Tests

        [Fact]
        public void DefaultInitialization_SetsDefaultValues()
        {
            // Arrange & Act
            var statsBar = new StatsBarData();

            // Assert
            Assert.Equal(0, statsBar.Value);
            Assert.Equal(100, statsBar.MaxValue);
            Assert.Equal("Default", statsBar.Name);
            Assert.False(statsBar.IsMaxReached);
            Assert.True(statsBar.IsMinReached);
        }

        [Fact]
        public void CustomInitialization_SetsCustomValues()
        {
            // Arrange
            var statsBar = new StatsBarData
            {
                Value = 50,
                MaxValue = 200,
                Name = "HealthBar"
            };

            // Act & Assert
            Assert.Equal(50, statsBar.Value);
            Assert.Equal(200, statsBar.MaxValue);
            Assert.Equal("HealthBar", statsBar.Name);
            Assert.False(statsBar.IsMaxReached);
            Assert.False(statsBar.IsMinReached);
        }

        #endregion

        #region Increment Tests

        [Fact]
        public void Increment_WithPositiveAmount_IncreasesCount()
        {
            // Arrange
            var statsBar = new StatsBarData();
            bool eventFired = false;
            statsBar.DataChanged += () => eventFired = true;

            // Act
            statsBar.Increment(30);

            // Assert
            Assert.Equal(30, statsBar.Value);
            Assert.False(statsBar.IsMaxReached);
            Assert.False(statsBar.IsMinReached);
            Assert.True(eventFired);
        }

        [Fact]
        public void Increment_WithNegativeAmount_IncreasesCountByAbsAmount()
        {
            // Arrange
            var statsBar = new StatsBarData
            {
                Value = 50
            };
            bool eventFired = false;
            statsBar.DataChanged += () => eventFired = true;

            // Act
            statsBar.Increment(-20);

            // Assert
            Assert.Equal(70, statsBar.Value);
            Assert.False(statsBar.IsMaxReached);
            Assert.False(statsBar.IsMinReached);
            Assert.True(eventFired);
        }

        [Fact]
        public void Increment_WithZeroAmount_DoesNotChangeCount_ButFiresDataChanged()
        {
            // Arrange
            var statsBar = new StatsBarData
            {
                Value = 50
            };
            bool eventFired = false;
            statsBar.DataChanged += () => eventFired = true;

            // Act
            statsBar.Increment(0);

            // Assert
            Assert.Equal(50, statsBar.Value);
            Assert.False(statsBar.IsMaxReached);
            Assert.False(statsBar.IsMinReached);
            Assert.True(eventFired); // DataChanged is still fired even if Count doesn't change
        }

        [Fact]
        public void Increment_ToMaxCount_SetsIsMaxReached()
        {
            // Arrange
            var statsBar = new StatsBarData
            {
                Value = 90,
                MaxValue = 100
            };
            bool eventFired = false;
            statsBar.DataChanged += () => eventFired = true;

            // Act
            statsBar.Increment(10);

            // Assert
            Assert.Equal(100, statsBar.Value);
            Assert.True(statsBar.IsMaxReached);
            Assert.False(statsBar.IsMinReached);
            Assert.True(eventFired);
        }

        [Fact]
        public void Increment_BeyondMaxCount_SetsCountToMax()
        {
            // Arrange
            var statsBar = new StatsBarData
            {
                Value = 95,
                MaxValue = 100
            };
            bool eventFired = false;
            statsBar.DataChanged += () => eventFired = true;

            // Act
            statsBar.Increment(10);

            // Assert
            Assert.Equal(100, statsBar.Value);
            Assert.True(statsBar.IsMaxReached);
            Assert.False(statsBar.IsMinReached);
            Assert.True(eventFired);
        }

        [Fact]
        public void Increment_WhenCountAtMax_DoesNotChange()
        {
            // Arrange
            var statsBar = new StatsBarData
            {
                Value = 100,
                MaxValue = 100
            };
            bool eventFired = false;
            statsBar.DataChanged += () => eventFired = true;

            // Act
            statsBar.Increment(10);

            // Assert
            Assert.Equal(100, statsBar.Value);
            Assert.True(statsBar.IsMaxReached);
            Assert.False(statsBar.IsMinReached);
            Assert.False(eventFired); // No change, event not fired
        }

        #endregion

        #region Decrement Tests

        [Fact]
        public void Decrement_WithPositiveAmount_DecreasesCount()
        {
            // Arrange
            var statsBar = new StatsBarData
            {
                Value = 50
            };
            bool dataChangedFired = false;
            bool onEqualZeroFired = false;
            statsBar.DataChanged += () => dataChangedFired = true;
            statsBar.OnEqualZero += () => onEqualZeroFired = true;

            // Act
            statsBar.Decrement(20);

            // Assert
            Assert.Equal(30, statsBar.Value);
            Assert.False(statsBar.IsMaxReached);
            Assert.False(statsBar.IsMinReached);
            Assert.True(dataChangedFired);
            Assert.False(onEqualZeroFired);
        }

        [Fact]
        public void Decrement_WithNegativeAmount_DecreasesCountByAbsAmount()
        {
            // Arrange
            var statsBar = new StatsBarData
            {
                Value = 50
            };
            bool dataChangedFired = false;
            bool onEqualZeroFired = false;
            statsBar.DataChanged += () => dataChangedFired = true;
            statsBar.OnEqualZero += () => onEqualZeroFired = true;

            // Act
            statsBar.Decrement(-20);

            // Assert
            Assert.Equal(30, statsBar.Value);
            Assert.False(statsBar.IsMaxReached);
            Assert.False(statsBar.IsMinReached);
            Assert.True(dataChangedFired);
            Assert.False(onEqualZeroFired);
        }

        [Fact]
        public void Decrement_WithZeroAmount_DoesNotChangeCount_ButFiresDataChanged()
        {
            // Arrange
            var statsBar = new StatsBarData
            {
                Value = 50
            };
            bool dataChangedFired = false;
            bool onEqualZeroFired = false;
            statsBar.DataChanged += () => dataChangedFired = true;
            statsBar.OnEqualZero += () => onEqualZeroFired = true;

            // Act
            statsBar.Decrement(0);

            // Assert
            Assert.Equal(50, statsBar.Value);
            Assert.False(statsBar.IsMaxReached);
            Assert.False(statsBar.IsMinReached);
            Assert.True(dataChangedFired); // DataChanged is still fired even if Count doesn't change
            Assert.False(onEqualZeroFired);
        }

        [Fact]
        public void Decrement_ToZero_SetsIsMinReached_AndFiresOnEqualZero()
        {
            // Arrange
            var statsBar = new StatsBarData
            {
                Value = 10
            };
            bool dataChangedFired = false;
            bool onEqualZeroFired = false;
            statsBar.DataChanged += () => dataChangedFired = true;
            statsBar.OnEqualZero += () => onEqualZeroFired = true;

            // Act
            statsBar.Decrement(10);

            // Assert
            Assert.Equal(0, statsBar.Value);
            Assert.False(statsBar.IsMaxReached);
            Assert.True(statsBar.IsMinReached);
            Assert.True(dataChangedFired);
            Assert.True(onEqualZeroFired);
        }

        [Fact]
        public void Decrement_BelowZero_SetsCountToZero_AndFiresOnEqualZero()
        {
            // Arrange
            var statsBar = new StatsBarData
            {
                Value = 5
            };
            bool dataChangedFired = false;
            bool onEqualZeroFired = false;
            statsBar.DataChanged += () => dataChangedFired = true;
            statsBar.OnEqualZero += () => onEqualZeroFired = true;

            // Act
            statsBar.Decrement(10);

            // Assert
            Assert.Equal(0, statsBar.Value);
            Assert.False(statsBar.IsMaxReached);
            Assert.True(statsBar.IsMinReached);
            Assert.True(dataChangedFired);
            Assert.True(onEqualZeroFired);
        }

        [Fact]
        public void Decrement_WhenCountAtZero_DoesNotChange()
        {
            // Arrange
            var statsBar = new StatsBarData
            {
                Value = 0
            };
            bool dataChangedFired = false;
            bool onEqualZeroFired = false;
            statsBar.DataChanged += () => dataChangedFired = true;
            statsBar.OnEqualZero += () => onEqualZeroFired = true;

            // Act
            statsBar.Decrement(10);

            // Assert
            Assert.Equal(0, statsBar.Value);
            Assert.False(statsBar.IsMaxReached);
            Assert.True(statsBar.IsMinReached);
            Assert.False(dataChangedFired);
            Assert.False(onEqualZeroFired);
        }

        #endregion

        #region Property Tests

        [Fact]
        public void IsMaxReached_WhenCountEqualsMax_ReturnsTrue()
        {
            // Arrange
            var statsBar = new StatsBarData
            {
                Value = 100,
                MaxValue = 100
            };

            // Act & Assert
            Assert.True(statsBar.IsMaxReached);
        }

        [Fact]
        public void IsMaxReached_WhenCountLessThanMax_ReturnsFalse()
        {
            // Arrange
            var statsBar = new StatsBarData
            {
                Value = 50,
                MaxValue = 100
            };

            // Act & Assert
            Assert.False(statsBar.IsMaxReached);
        }

        [Fact]
        public void IsMinReached_WhenCountEqualsZero_ReturnsTrue()
        {
            // Arrange
            var statsBar = new StatsBarData
            {
                Value = 0
            };

            // Act & Assert
            Assert.True(statsBar.IsMinReached);
        }

        [Fact]
        public void IsMinReached_WhenCountGreaterThanZero_ReturnsFalse()
        {
            // Arrange
            var statsBar = new StatsBarData
            {
                Value = 10
            };

            // Act & Assert
            Assert.False(statsBar.IsMinReached);
        }

        #endregion

        #region Event Tests

        [Fact]
        public void Increment_FiresDataChangedEvent()
        {
            // Arrange
            var statsBar = new StatsBarData();
            bool eventFired = false;
            statsBar.DataChanged += () => eventFired = true;

            // Act
            statsBar.Increment(10);

            // Assert
            Assert.True(eventFired);
        }

        [Fact]
        public void Increment_DoesNotFireDataChangedEvent_WhenCountDoesNotChange()
        {
            // Arrange
            var statsBar = new StatsBarData
            {
                Value = 100,
                MaxValue = 100
            };
            bool eventFired = false;
            statsBar.DataChanged += () => eventFired = true;

            // Act
            statsBar.Increment(10);

            // Assert
            Assert.False(eventFired);
        }

        [Fact]
        public void Decrement_FiresDataChangedEvent()
        {
            // Arrange
            var statsBar = new StatsBarData
            {
                Value = 50
            };
            bool eventFired = false;
            statsBar.DataChanged += () => eventFired = true;

            // Act
            statsBar.Decrement(10);

            // Assert
            Assert.True(eventFired);
        }

        [Fact]
        public void Decrement_FiresOnEqualZeroEvent_WhenCountReachesZero()
        {
            // Arrange
            var statsBar = new StatsBarData
            {
                Value = 10
            };
            bool onEqualZeroFired = false;
            statsBar.OnEqualZero += () => onEqualZeroFired = true;

            // Act
            statsBar.Decrement(10);

            // Assert
            Assert.True(onEqualZeroFired);
        }

        [Fact]
        public void Decrement_DoesNotFireOnEqualZeroEvent_WhenCountDoesNotReachZero()
        {
            // Arrange
            var statsBar = new StatsBarData
            {
                Value = 20
            };
            bool onEqualZeroFired = false;
            statsBar.OnEqualZero += () => onEqualZeroFired = true;

            // Act
            statsBar.Decrement(10);

            // Assert
            Assert.False(onEqualZeroFired);
        }

        [Fact]
        public void Decrement_DoesNotFireOnEqualZeroEvent_WhenAmountDoesNotReachZero()
        {
            // Arrange
            var statsBar = new StatsBarData
            {
                Value = 20
            };
            bool onEqualZeroFired = false;
            statsBar.OnEqualZero += () => onEqualZeroFired = true;

            // Act
            statsBar.Decrement(10);

            // Assert
            Assert.False(onEqualZeroFired);
        }

        [Fact]
        public void Decrement_FiresOnEqualZeroEvent_WhenCountAlreadyZero_DoesNotFire()
        {
            // Arrange
            var statsBar = new StatsBarData
            {
                Value = 0
            };
            bool onEqualZeroFired = false;
            statsBar.OnEqualZero += () => onEqualZeroFired = true;

            // Act
            statsBar.Decrement(10);

            // Assert
            Assert.False(onEqualZeroFired);
        }

        #endregion
    }
}
