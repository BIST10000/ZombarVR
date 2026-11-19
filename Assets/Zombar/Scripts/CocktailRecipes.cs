using UnityEngine;

namespace Zombar
{
    public static class CocktailRecipes
    {
        public static readonly string[] Names = {
            "Кровавый флип", "Гнилой сауэр", "Глазной хайбол",
            "Двойная кровь", "Красный взгляд", "Мясной удар",
            "Третий глаз", "Моргтини", "Последний ужин"
        };
        public static readonly Vector3Int[] Portions = {
            new Vector3Int(1,1,0), new Vector3Int(1,0,1), new Vector3Int(0,1,1),
            new Vector3Int(2,1,0), new Vector3Int(1,2,0), new Vector3Int(2,0,1),
            new Vector3Int(0,2,1), new Vector3Int(1,1,1), new Vector3Int(1,0,2)
        };
        public static int Match(int blood, int eyes, int flesh)
        {
            return System.Array.IndexOf(Portions, new Vector3Int(blood, eyes, flesh));
        }
        public static string Formula(int index)
        {
            if (index < 0 || index >= Portions.Length) return "";
            var p = Portions[index];
            var parts = new System.Collections.Generic.List<string>(3);
            if (p.x > 0) parts.Add($"Кровь ×{p.x}");
            if (p.y > 0) parts.Add($"Глаз ×{p.y}");
            if (p.z > 0) parts.Add($"Плоть ×{p.z}");
            return string.Join("  ·  ", parts);
        }
    }
}

