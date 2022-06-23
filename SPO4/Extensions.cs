namespace SPO4
{
    public static class Extensions
    {
        /// <summary>
        /// Метод проверяет совпадение символа с любым из переданных в массиве.
        /// </summary>
        /// <param name="ch">Символ, который необходимо сравнить.</param>
        /// <param name="chars">Массив символов.</param>
        /// <returns></returns>
        public static bool IsAnyOf<T>(this T obj, params T[] list)
        {
            for (var idx = list.Length - 1; idx >= 0; idx--)
                if (list[idx].Equals(obj))
                    return true;

            return false;
        }
    }
}
