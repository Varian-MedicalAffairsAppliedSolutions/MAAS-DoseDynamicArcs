using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VirtualCones_MCB.Models
{
    public class ProgressItem
    {
        public ProgressItem()
        {
        }

        public ProgressItem(int currentItem, int totalItemCount, string message)
        {
            CurrentItem = currentItem;
            TotalItemCount = totalItemCount;
            Message = message;
            PercentageCalculate();
        }

        public int CurrentItem { get; set; }
        public int TotalItemCount { get; set; }
        public string Message { get; set; }
        public double Percentage { get; set; }

        public void PercentageCalculate()
        {
            try
            {
                Percentage = Math.Round(100.0 * CurrentItem / TotalItemCount, 1);
            }
            catch (Exception ex)
            {
                Percentage = 0;
            }
        }
    }
}
