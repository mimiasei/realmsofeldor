using NUnit.Framework;
using RealmsOfEldor.Core;
using RealmsOfEldor.Data;

namespace RealmsOfEldor.Tests
{
    [TestFixture]
    public class HeroTests
    {
        [Test]
        public void Hero_GainExperience_IncreasesExperience()
        {
            // Arrange
            var hero = new Hero { Level = 1, Experience = 0 };

            // Act
            hero.GainExperience(500);

            // Assert
            Assert.AreEqual(500, hero.Experience);
        }

        [Test]
        public void Hero_GainExperience_LevelsUp()
        {
            // Arrange
            var hero = new Hero { Level = 1, Experience = 0 };

            // Act
            hero.GainExperience(1000); // Level 1 requires 1000 exp

            // Assert
            Assert.AreEqual(2, hero.Level);
        }

        [Test]
        public void Hero_GainExperience_MultipleLevelUps()
        {
            // Arrange
            var hero = new Hero { Level = 1, Experience = 0 };

            // Act
            hero.GainExperience(3000); // Should level up to 3 (1000 + 2000)

            // Assert
            Assert.AreEqual(3, hero.Level);
        }

        [Test]
        public void Hero_RefreshMovement_RestoresMovementPoints()
        {
            // Arrange
            var hero = new Hero { MaxMovement = 1500, Movement = 500 };

            // Act
            hero.RefreshMovement();

            // Assert
            Assert.AreEqual(1500, hero.Movement);
        }

        [Test]
        public void Hero_SpendMovement_ReducesMovementPoints()
        {
            // Arrange
            var hero = new Hero { Movement = 1000 };

            // Act
            hero.SpendMovement(300);

            // Assert
            Assert.AreEqual(700, hero.Movement);
        }

        [Test]
        public void Hero_SpendMovement_DoesNotGoBelowZero()
        {
            // Arrange
            var hero = new Hero { Movement = 100 };

            // Act
            hero.SpendMovement(200);

            // Assert
            Assert.AreEqual(0, hero.Movement);
        }

        [Test]
        public void Hero_CanMove_ReturnsTrueWhenEnoughMovement()
        {
            // Arrange
            var hero = new Hero { Movement = 500 };

            // Act & Assert
            Assert.IsTrue(hero.CanMove(300));
        }

        [Test]
        public void Hero_CanMove_ReturnsFalseWhenNotEnoughMovement()
        {
            // Arrange
            var hero = new Hero { Movement = 100 };

            // Act & Assert
            Assert.IsFalse(hero.CanMove(200));
        }

        [Test]
        public void Hero_LearnSpell_AddsSpellToKnownSpells()
        {
            // Arrange
            var hero = new Hero { HasSpellbook = true };

            // Act
            bool result = hero.LearnSpell(1);

            // Assert
            Assert.IsTrue(result);
            Assert.IsTrue(hero.KnowsSpell(1));
        }

        [Test]
        public void Hero_LearnSpell_FailsWithoutSpellbook()
        {
            // Arrange
            var hero = new Hero { HasSpellbook = false };

            // Act
            bool result = hero.LearnSpell(1);

            // Assert
            Assert.IsFalse(result);
            Assert.IsFalse(hero.KnowsSpell(1));
        }

        [Test]
        public void Hero_AddSecondarySkill_AddsNewSkill()
        {
            // Arrange
            var hero = new Hero();

            // Act
            hero.AddSecondarySkill(SecondarySkillType.Archery, SkillLevel.Basic);

            // Assert
            Assert.AreEqual(SkillLevel.Basic, hero.GetSecondarySkillLevel(SecondarySkillType.Archery));
        }

        [Test]
        public void Hero_AddSecondarySkill_UpgradesExistingSkill()
        {
            // Arrange
            var hero = new Hero();
            hero.AddSecondarySkill(SecondarySkillType.Archery, SkillLevel.Basic);

            // Act
            hero.AddSecondarySkill(SecondarySkillType.Archery, SkillLevel.Advanced);

            // Assert
            Assert.AreEqual(SkillLevel.Advanced, hero.GetSecondarySkillLevel(SecondarySkillType.Archery));
        }

        [Test]
        public void Hero_SpendMana_ReducesMana()
        {
            // Arrange
            var hero = new Hero { Mana = 50, HasSpellbook = true };

            // Act
            bool result = hero.SpendMana(20);

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(30, hero.Mana);
        }

        [Test]
        public void Hero_SpendMana_FailsWhenNotEnoughMana()
        {
            // Arrange
            var hero = new Hero { Mana = 10, HasSpellbook = true };

            // Act
            bool result = hero.SpendMana(20);

            // Assert
            Assert.IsFalse(result);
            Assert.AreEqual(10, hero.Mana);
        }
    }
}
