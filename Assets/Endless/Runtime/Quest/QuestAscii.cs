using System;
using System.Linq;
using System.Text;

namespace Pawnshop.Endless
{
    public static class QuestAscii
    {
        public static string Render(QuestRules quest, int selected, int tick, bool street)
        {
            if (quest.State.phase == DayPhase.Night) return Ritual(quest, selected, tick);
            if (quest.State.phase == DayPhase.Morning) return street ? @"
       .     .     .     .
      /|________________/|
      ||                ||
      ||   .------.     ||
      ||  /  _  _  \    ||
      ||  |  o  o  |    ||
      ||  |   __   |    ||
      ||   \__~~__/     ||
      ||   /|    |\     ||
      ||  / |____| \    ||
      || /  |    |  \   ||
   ___||/___|____|___\__||___
      /    [____]      /
     /________________/

          Б О Р И С
       за стеной магазина
" : @"
        ___________________
       / Н И Н А / 08:00  /|
      /__________________/ |
      | .--. .--. .--.   | |
      | |::| |::| |::|   | |
      | '--' '--' '--'   | |
      |       .----.     | |
      |      / .  . \    | |
      |      |  __  |    | |
      |       \____/     | |
      |      /|/\/|\     | |
      |_____/________\___|/
      | СОЛЬ | МОЛОКО |  |
      | [::] |  [__]  |  |
      |__________________|
      / / / / / / / / / /

        открыто до обеда
";
            if (quest.State.phase == DayPhase.Evening) return @"
         .------------------.
         |  ОБЪЯВЛЕНИЯ      |
         |------------------|
         |  [o]  ВЕЩЬ ИЩЕТ  |
         |       ХОЗЯИНА    |
         |                  |
         |  ..............  |
         |  ...........     |
         |                  |
         |   [ РАЗМЕСТИТЬ ] |
         |__________________|
             /________\
         ___/__________\___
         |________________|

           19:40 / в сети
      Кто-то сохранил ваше фото.
";
            if (quest.State.policePending) return @"
              __________
             /__________\
             |  _    _  |
             |  o    o  |
             |    __    |
              \________/
              /|  /\  |\
             / | /  \ | \
       _____/__|/____\|__\____
       |   ДЕЛО  /  КОЛЬЦО   |
       |   [ФОТО ОБЪЯВЛЕНИЯ] |
       |_____________________|

       «Начнём с самого начала».
";
            return quest.State.doorOpened ? @"
        |\                   |
        | \      .----.      |
        |  \    /      \     |
        |   |   | o  o |     |
        |   |    \ -- /      |
        |   |   __|__|__     |
        | o |  /        \    |
        |   | /   .--.   \   |
        |   | |___|??|___|   |
        |   |     '--'       |
        |   /                |
        |  /_________________|
        | /
        |/

       У каждой вещи своя версия.
" : $@"
          __________________
         |  ______________  |
         | |              | |
         | |              | |
         | |      {((tick % 3 == 0) ? "*" : " ")}       | |
         | |              | |
         | |______________| |
         |  ______________  |
         | |              |o|
         | |              | |
         | |              | |
         | |______________| |
         |__________________|

            т у к . . .
";
        }
        static string Ritual(QuestRules quest, int selected, int tick)
        {
            const int width = 38, height = 25;
            var canvas = new char[height, width];
            for (int y = 0; y < height; y++) for (int x = 0; x < width; x++) canvas[y,x] = ' ';
            var item = quest.Shop.State.stock.FirstOrDefault(x => x.id == selected);
            var curse = item?.curse ?? Curse.None;
            var points = new[] { (19,3), (28,21), (4,10), (34,10), (10,21) };
            var progress = quest.State.ritualId == selected ? quest.State.saltPoints : 0;
            for (int line = 0; line < 5; line++)
            {
                var a = points[line]; var b = points[(line+1)%5];
                int steps = Math.Max(Math.Abs(a.Item1-b.Item1), Math.Abs(a.Item2-b.Item2));
                for (int i = 0; i <= steps; i++)
                {
                    int x = a.Item1 + (b.Item1-a.Item1)*i/steps, y = a.Item2 + (b.Item2-a.Item2)*i/steps;
                    canvas[y,x] = line < progress ? '+' : '.';
                }
                canvas[a.Item2,a.Item1] = line < progress ? '*' : (char)('1'+line);
            }
            for (int y = 0; y < height; y++) for (int x = 0; x < width; x++)
            {
                if (canvas[y,x] != ' ') continue;
                if (curse == Curse.Cold && (x*7+y*11+tick)%31 == 0) canvas[y,x] = new[] {'*','+','x'}[(x+y)%3];
                if (curse == Curse.Whisper && (y+tick)%5 == 0 && (x+tick)%7 < 2) canvas[y,x] = "НЕСПИ"[(x+y+tick)%5];
                if (curse == Curse.Hunger)
                {
                    double dx = (x-19)/1.8, dy = y-13;
                    double ring = Math.Sqrt(dx*dx+dy*dy);
                    if ((int)(ring+tick%4)%5 == 0) canvas[y,x] = ':';
                }
            }
            string center = curse == Curse.Hunger ? "< ( O ) >" : curse == Curse.Cold ? "[ * O * ]" : curse == Curse.Whisper ? "{ ? O ? }" : "[   O   ]";
            for (int i = 0; i < center.Length; i++) canvas[13,15+i] = center[i];
            if (quest.State.milk && quest.State.ritualId == selected)
                for (int x = 12; x < 27; x++) canvas[16,x] = quest.State.boiled ? ((x+tick)%2==0 ? 'o' : '~') : '~';
            var result = new StringBuilder("         РИТУАЛЬНЫЙ СТОЛ\n\n");
            for (int y = 0; y < height; y++) { for (int x = 0; x < width; x++) result.Append(canvas[y,x]); result.Append('\n'); }
            result.Append(curse == Curse.None ? "           . т и ш и н а ." : "       не размыкайте последнюю линию");
            return result.ToString();
        }
    }
}
