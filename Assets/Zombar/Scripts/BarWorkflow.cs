using UnityEngine;

namespace Zombar
{
    public sealed class BarWorkflow : MonoBehaviour
    {
        private LineRenderer intake, handoff;
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
            handoff = Instantiate(intake, transform);
            handoff.name = "Место подачи бокала";
            handoff.transform.rotation = Quaternion.identity;
            handoff.enabled = false;
        }

        private void LateUpdate()
        {
            var game = ZombieBarGame.Instance;
            var shaker = PhysicalBar.Instance.shaker;
            if (game == null || game.Hud == null || shaker == null) return;
            intake.enabled = !game.GameOver && shaker.recipe < 0 && !shaker.IsHeld;
            int order = game.CurrentOrder;
            string title = order < 0 ? "Ожидайте гостя" : CocktailRecipes.Names[order];
            string step;
            var glass = BarProp.All.Find(p => p.kind == PropKind.Glass && p.recipe >= 0);
            var recipient = glass == null ? null : PhysicalPatron.All.Find(p => !p.Defeated && !p.Approaching && p.order == glass.recipe);
            handoff.enabled = !game.GameOver && recipient != null;
            if (recipient != null)
            {
                intake.enabled = false;
                handoff.transform.position = recipient.ReceivePosition - Vector3.up * .33f;
                order = glass.recipe;
                title = CocktailRecipes.Names[order];
            }
            if (game.GameOver) step = "Смена окончена. Возьмите колокольчик и встряхните.";
            else if (PhysicalBar.Instance.GetComponent<BarMixer>()?.Running == true) step = "Миксер работает. Подождите готовности.";
            else if (glass != null) step = recipient != null ? "Отпустите бокал в кольце " + (recipient.transform.position.x < 0 ? "слева" : "справа") : "Сейчас такой напиток никто не заказывал";
            else if (shaker.recipe >= 0) step = "Поднесите пустой бокал справа к чаше.";
            else if (order >= 0 && CocktailRecipes.Portions[order] == new Vector3Int(shaker.blood, shaker.eyes, shaker.flesh))
                step = "Чашу в гнездо. Нажмите кнопку миксера.";
            else if (order >= 0 && (shaker.blood > CocktailRecipes.Portions[order].x ||
                shaker.eyes > CocktailRecipes.Portions[order].y || shaker.flesh > CocktailRecipes.Portions[order].z))
                step = "Лишний ингредиент.\nНаклоните шейкер над сливом справа.";
            else step = "Добавьте ингредиенты в кольцо чаши.";
            var guest = recipient != null ? recipient : PhysicalPatron.All.Find(p => !p.Defeated && !p.Approaching && p.order == order);
            string station = guest == null ? "НЕКРО-ПАБ" : guest.transform.position.x < 0 ? "ЗАКАЗ   /   СЛЕВА" : "ЗАКАЗ   /   СПРАВА";
            var portions = new Vector3Int(shaker.blood, shaker.eyes, shaker.flesh);
            int stage = glass != null || shaker.recipe >= 0 ? 2 : order >= 0 && portions == CocktailRecipes.Portions[order] ? 1 : 0;
            game.Hud.SetWorkflow(station, title, CocktailRecipes.Formula(order), step, stage);
        }

        private void OnDestroy() { if (markerMaterial != null) Destroy(markerMaterial); }
    }
}


