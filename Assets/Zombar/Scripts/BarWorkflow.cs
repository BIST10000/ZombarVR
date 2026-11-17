using UnityEngine;

namespace Zombar
{
    public sealed class BarWorkflow : MonoBehaviour
    {
        private LineRenderer intake;
        private Material markerMaterial;

        private void Start()
        {
            var obj = new GameObject("Горловина загрузки");
            obj.transform.SetParent(PhysicalBar.Instance.shaker.transform, false);
            intake = obj.AddComponent<LineRenderer>();
            markerMaterial = new Material(Resources.Load<Material>("PhysicalBar/BloodStream"));
            markerMaterial.color = new Color(.3f, .9f, .65f);
            intake.sharedMaterial = markerMaterial;
            intake.useWorldSpace = false;
            intake.loop = true;
            intake.widthMultiplier = .006f;
            intake.positionCount = 48;
            for (int i = 0; i < 48; i++)
            {
                float angle = i * Mathf.PI * 2 / 48;
                intake.SetPosition(i, new Vector3(Mathf.Cos(angle)*.09f, .25f, Mathf.Sin(angle)*.09f));
            }
        }

        private void LateUpdate()
        {
            var game = ZombieBarGame.Instance;
            var shaker = PhysicalBar.Instance.shaker;
            if (game == null || game.Hud == null || shaker == null) return;
            intake.enabled = !game.GameOver && shaker.recipe < 0 && !shaker.IsHeld;
            int order = game.CurrentOrder;
            string title = order < 0 ? "Ожидайте гостя" : CocktailRecipes.Names[order] + "\n" + CocktailRecipes.Formula(order);
            string step;
            var glass = BarProp.All.Find(p => p.kind == PropKind.Glass && p.recipe >= 0);
            if (game.GameOver) step = "Смена окончена. Возьмите колокольчик и встряхните.";
            else if (PhysicalBar.Instance.GetComponent<BarMixer>()?.Running == true) step = "Миксер работает. Подождите готовности.";
            else if (glass != null) step = "Возьмите наполненный бокал.\nПоднесите его к клиенту с таким заказом.";
            else if (shaker.recipe >= 0) step = "Возьмите шейкер.\nНаклоните горлышко над бокалом справа.";
            else if (order >= 0 && CocktailRecipes.Portions[order] == new Vector3Int(shaker.blood, shaker.eyes, shaker.flesh))
                step = "Поставьте стакан в гнездо.\nНажмите большую кнопку справа на основании.";
            else if (order >= 0 && (shaker.blood > CocktailRecipes.Portions[order].x ||
                shaker.eyes > CocktailRecipes.Portions[order].y || shaker.flesh > CocktailRecipes.Portions[order].z))
                step = "Лишний ингредиент.\nНаклоните шейкер над сливом справа.";
            else step = "Поднесите глаз / плоть к открытому верху.\nКровь налейте из фляги, наклонив её над кольцом.";
            game.Hud.SetWorkflow(title + "\n" + $"В шейкере · кровь {shaker.blood}, глаз {shaker.eyes}, плоть {shaker.flesh}" + "\n" + step);
        }

        private void OnDestroy() { if (markerMaterial != null) Destroy(markerMaterial); }
    }
}

