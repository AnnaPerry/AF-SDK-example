using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using OSIsoft.AF;
using OSIsoft.AF.Asset;
using OSIsoft.AF.Search;
using OSIsoft.AF.Time;
using OSIsoft.AF.Data;

namespace DCS_Tech_Tool
{

    class DataItem
    {      
        public string Completion { get; set; }
        public AFAttribute Attribute { get; set; }
        public string Instrument { get; set; }
        public AFAttribute InstrumentAttribute { get; set; }
        public AFValue InstrumentValue { get; set; }
        public string Status { get; set; }

        public DataItem(string completionname, AFAttribute instrumentparentname, string instrumentvaluestring, AFAttribute instrumentattribute, AFValue instrumentvalue, string status)
        {
            this.Completion = completionname;
            this.Attribute = instrumentparentname;
            this.Instrument = instrumentvaluestring;
            this.InstrumentAttribute = instrumentattribute;
            this.InstrumentValue = instrumentvalue;          
            this.Status = status;
        }
    }


    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        


        private void DataGrid_Loaded(object sender, RoutedEventArgs e)
        {
            PISystem _afserver = new PISystems()["ARCADIA"]; //AF Server
            AFDatabase _afdb = _afserver.Databases["DCS Tool DB"]; //AF Database

            
            AFElementTemplate elementTemplate = _afdb.ElementTemplates["Completion"];
            AFSearchToken templateToken = new AFSearchToken(AFSearchFilter.Template, AFSearchOperator.Equal, elementTemplate.GetPath());
            AFElementSearch elementSearch = new AFElementSearch(_afdb, "FindCompletionElements", new[] { templateToken });
            var elements = elementSearch.FindElements(0, false, 1000).ToList();

            AFElement.LoadElements(elements);

            var attributes = elements.SelectMany(elem => elem.Attributes.OfType<AFAttribute>());

            var piDataReferencePluginID = AFDataReference.GetPIPointDataReference(_afserver).ID;
            var piAttributes = attributes.Where(a =>
                                                a.DataReferencePlugIn != null
                                                && a.DataReferencePlugIn.ID == piDataReferencePluginID).ToList();

            var childattributes = piAttributes.SelectMany(att => att.Attributes.OfType<AFAttribute>());

            List<DataItem> dataItems = new List<DataItem>();

            var instrumentattributes = childattributes.Where(att => att.Name == "Instrument Tag").ToList();
            foreach (var instrumentatt in instrumentattributes) {
                var completion = instrumentatt.Element.Name;
                var attribute = instrumentatt.Parent;
                var instrument = instrumentatt.GetValue();
                var status = instrumentatt.Parent.GetValue().ToString();           

                dataItems.Add(new DataItem(completion, attribute, instrument.Value.ToString(), instrumentatt, instrument, status));
            }


            var grid = sender as DataGrid;
           

            grid.ItemsSource = dataItems;
            grid.Columns.Where(col => col.Header.ToString() != "Instrument")
                        .ToList()
                        .ForEach(col => col.IsReadOnly = true);
            grid.Columns.Where(col => col.Header.ToString() == "InstrumentAttribute" || col.Header.ToString() == "InstrumentValue")
                        .ToList()
                        .ForEach(col => col.Visibility = Visibility.Hidden);

        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            var selectedDataItems = dataGrid.SelectedItems.Cast<DataItem>().ToList();
            List<AFValue> newValues = new List<AFValue>();

            var values = selectedDataItems.Where(item => item.Instrument != item.InstrumentValue.Value.ToString())
                                        .Select(item => new AFValue(item.InstrumentAttribute,item.Instrument, new AFTime("*")))
                                        .ToList();
            if (values.Count > 0)
            {
                var errors = AFListData.UpdateValues(values, AFUpdateOption.Insert);

                var attributes = values
                            .Select(item => item.Attribute.Parent)
                            .ToList();
                AFDataReference.CreateConfig(attributes, false, null);

            }

        }


    }
}
