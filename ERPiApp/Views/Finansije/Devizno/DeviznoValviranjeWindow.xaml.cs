using System;
using System.Windows;
using ERPiData;

namespace ERPiApp.Views.Finansije.Devizno;

public partial class DeviznoValviranjeWindow : Window
{
    public DeviznoValviranjeWindow(ErpiDbContext db)
    {
        InitializeComponent();

        var view = new DeviznoValviranjeView(db, prikaziDugmeZatvori: true);
        view.CloseRequested += (s, e) => { DialogResult = true; Close(); };
        ContentHost.Content = view;
    }
}
