namespace RealmsOfEldor.Core.Battle
{
    /// <summary>
    /// Simple data structure for passing stack visual data.
    /// Used to adapt BattleUnit to the visual layer.
    /// </summary>
    public class BattleStack
    {
        public int Id { get; set; }
        public int CreatureId { get; set; }
        public int Count { get; set; }
        public BattleSide Side { get; set; }
        public BattleHex Position { get; set; }
    }
}
