using System.Collections.Generic;
using System.Data.SqlTypes;

namespace GenTreesCore.Entities
{
    public class GenTreeDateTime : INullable, IIdentified
    {
        public bool IsNull { get; }
        public int Id { get; set; }
        public GenTreeEra Era { get; set; }
        public int Year { get; set; }
        public int Month { get; set; }
        public int Day { get; set; }
        public int Hour { get; set; }
        public int Minute { get; set; }
        public int Second { get; set; }

        public string ToDateTimeString()
        {
            return $"{Era.Name}, {Day}/{Month}/{Year} {Hour}:{Minute}:{Second}";
        }

        public string ToShortDateTimeString()
        {
            return $"{Day}/{Month}/{Year}, {Era.ShortName}";
        }
    }

    /// <summary>
    /// Настройка летоисчисления
    /// </summary>
    public class GenTreeDateTimeSetting : IIdentified
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public User Owner { get; set; }
        public bool IsPrivate { get; set; }
        public List<GenTreeEra> Eras { get; set; }
        /// <summary>
        /// Кол-во месяцев в году
        /// TODO настройка каждого месяца, чтобы можно было поменять его название и кол-во дней в месяце
        /// </summary>
        public int YearMonthCount { get; set; }
    }

    /// <summary>
    /// Эпоха летоисчисления
    /// </summary>
    public class GenTreeEra : IIdentified
    {
        public int Id { get; set; }
        /// <summary>
        /// Название эпохи
        /// Например, "Первая Эпоха" или "Эпоха Древ"
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Краткое название эпохи, используется в подписях годов
        /// Например, "П.Э." или "Э.Д."
        /// </summary>
        public string ShortName { get; set; }
        /// <summary>
        /// Справочная информация по эпохе
        /// </summary>
        public string Description { get; set; }
        /// <summary>
        /// Год начала эпохи в сквозном летоисчислении (если бы не было разделения на эпохи)
        /// </summary>
        public int ThroughBeginYear { get; set; }
        /// <summary>
        /// Длительность эпохи в годах
        /// </summary>
        public int? YearCount { get; set; }
    }
}
