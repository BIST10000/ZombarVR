# Готовые персонажи текущей версии

- Warzombie_F_Pedroso.blend — обычный клиент в военной форме.
- Zombiegirl_W_Kurniawan.blend — обычная клиентка.
- Yaku_J_Ignite.blend — помощник.
- Mutant.blend — неуязвимый титан.

Это копии исходных моделей Adobe Mixamo с упакованными текстурами и скелетами. Игровые FBX находятся в Assets/Zombar/Art/External/Mixamo. Их используют префабы PatronMale, PatronFemale, Helper и Titan в Assets/Zombar/Resources/PhysicalBar.

После ручных правок экспортируйте скелет и меши через File → Export → FBX поверх соответствующего игрового FBX. Forward -Z, Up Y, Add Leaf Bones выключен, Bake Animation выключен. Имена костей и материалов нужны для существующих привязок. Анимации находятся в отдельной папке Animations и воспроизводятся через Humanoid в Unity. Нормализованный рост задаётся префабом.

Для изменения используемой Unity текстуры сохраните PNG в Assets/Zombar/Art/External/Mixamo под её текущим именем. Изменения упакованной текстуры Blender сохраняются отдельно от PNG в Unity.
