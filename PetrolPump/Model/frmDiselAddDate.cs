using System;

namespace AlMadinaShahzad.Model
{
    public partial class frmDiselAdd
    {
        private void ResetFormKeepSelectedDate()
        {
            DateTime selectedDate = txtdate.Value;
            MainClass.Enable_reset(this);
            txtdate.Value = selectedDate;
        }
    }
}
