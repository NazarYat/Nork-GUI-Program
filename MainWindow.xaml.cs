using System;
using System.Text.RegularExpressions;
using System.ComponentModel;
using System.Text.Json;
using System.Text;
using Nork;
using Nork.DatasetCreator;
using Nork.StatisticsCalculator;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;
using System.Windows.Controls;
using System.Windows.Input;
using OxyPlot;

namespace NGP {
    public partial class MainWindow : Window {
        public MainWindow() {
            Loaded += SetModelData;
            Loaded += LoadProgramData;
            Loaded += OpenRecent;
            Closing += SaveProgramData;

            InitializeComponent();
        }
        public MainWindow( string path ) {
            Loaded += SetModelData;
            Loaded += LoadProgramData;
            Closing += SaveProgramData;

            InitializeComponent();

            SetFilesPaths( path );
        }
        void OpenRecent( object sender, RoutedEventArgs e ) {
            var recentFilePath = Model.ProgramData.RecentFilePath;
            if ( File.Exists( recentFilePath ) ) {
                SetFilesPaths( recentFilePath );
            }
        }
        void LoadProgramData( object sender, RoutedEventArgs e ) {
            Model.ProgramData = Model.ProgramData.Get();
        }
        void SaveProgramData( object sender, CancelEventArgs e ) {
            Model.ProgramData.Save();
        }
        void SetFilesPaths( string startPath ) {
            var startPathType = FileProcess.GetType( startPath );
            if ( startPathType == "nns" ) {
                Model.NeuralNetworkFilePath = startPath;
                Model.NeuralNetworkFileName = FileProcess.GetFullName( startPath );
                var tdsFilePath = FileProcess.GetPath( startPath ) + "/" + FileProcess.GetName( startPath ) + ".dts";
                if ( File.Exists( tdsFilePath ) ) {
                    Model.DatasetFilePath = tdsFilePath;
                    Model.DatasetFileName = FileProcess.GetFullName( Model.DatasetFilePath );
                }
            }
            else if ( startPathType == "dts" ) {
                Model.DatasetFilePath = startPath;
                Model.DatasetFileName = FileProcess.GetFullName( startPath );
            }
            SetFilesData();
        }
        void SetFilesData() {
            if ( Model.NeuralNetworkFilePath != null ) {
                Model.ProgramData.RecentFilePath = Model.NeuralNetworkFilePath;
                Model.NeuralNetwork = NeuralNetworkSaver.Get( Model.NeuralNetworkFilePath );
                Model.NeuralNetworkType = Model.NeuralNetwork.Options.Type.ToString();
                Model.InputNeuronsCount = Model.NeuralNetwork.InputLayer.GetOutValues().Count;
                Model.OutputNeuronsCount = Model.NeuralNetwork.OutputLayer.GetOutValues().Count;
                Model.LearningSpeed = Model.NeuralNetwork.Options.LearningSpeed;
                Model.Moment = Model.NeuralNetwork.Options.Moment;
            }
            if ( Model.DatasetFilePath != null ) {
                Model.Dataset = DatasetSaver.Get( Model.DatasetFilePath );
                Model.DatasetFramesCount = Model.Dataset.Frames.Count;
            }
        }
        async void SetModelData( object sender, RoutedEventArgs e ) {
            while ( true ) {
                try {
                    OpenedDatasetNameLabel.Content = Model.DatasetFileName;
                    OpenedNeuralNetworkNameLabel.Content = Model.NeuralNetworkFileName;
                    InputNeuronsCountLabel.Content = Model.InputNeuronsCount;
                    OutputNeuronsCountLabel.Content = Model.OutputNeuronsCount;
                    DatasetFramesCountLabel.Content = Model.DatasetFramesCount;
                    LearningSpeedLabel.Content = Model.LearningSpeed;
                    MomentLabel.Content = Model.Moment;
                    IterationLabel.Content = Model.Iteration;
                    EraLabel.Content = Model.Era;
                    NeuralNetworkTypeLabel.Content = Model.NeuralNetworkType;

                    if ( Model.LearningMode == Model.Learning.Mode.Error || Model.LearningMode == Model.Learning.Mode.Eras ) {
                        ErrorNameLabel.Content = "Error";
                        ErrorLabel.Content = Model.Error;
                    }
                    else {
                        ErrorNameLabel.Content = "Accurancy";
                        ErrorLabel.Content = Model.Error;
                    }

                    IterationTimeLabel.Content =  Model.IterationTime;
                    EraTimeLabel.Content = Model.EraTime;
                    LearningTimeLabel.Content = Model.LearningTime.ToString();
                    await Tools.Chart.DrawAsync( Model.NeuralNetworkErrorHistory, NeuralNetworkChart );

                    if ( !Model.Learn & LearnButton.Content.ToString() != "learn" ) {
                        LearnButton.Content = "learn";
                        NeeuralNetworkLearner.Timer.Stop();
                    }
                }
                catch {}

                await Task.Delay(20);
            }
        }
        void OpenNeuralNetwork( object sender, RoutedEventArgs e ) {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Neural network (*.nns)|*.nns";
            openFileDialog.InitialDirectory = @"c:\";
			if ( openFileDialog.ShowDialog() == true ) {
                var targetFilePath = openFileDialog.FileName;
				SetFilesPaths( targetFilePath );
            }
        }
        void OpenDataset( object sender, RoutedEventArgs e ) {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Datatset (*.dts)|*.dts";
            openFileDialog.InitialDirectory = @"c:\";
			if ( openFileDialog.ShowDialog() == true ) {
                var targetFilePath = openFileDialog.FileName;
				SetFilesPaths( targetFilePath );
            }
        }
        void OnLearningTargetChange( object sender, TextChangedEventArgs e ) {
            var text = ( (TextBox) sender ).Text;

            if ( text.Length > 0 ) {
                var lastTextSymmbol = text[ text.Length - 1 ];
                if ( lastTextSymmbol != ',' ) {
                    Model.LearningTarget = Convert.ToDouble( ( (TextBox) sender ).Text );
                }
            }
        }
        void LearnButtonClicked( object sender, RoutedEventArgs e ) {
            var button = (Button) sender;

            if ( button.Content.ToString() == "learn" ) {
                NeeuralNetworkLearner.StartLearn();
                NeeuralNetworkLearner.Timer.Start();
                button.Content = "stop";
            }
            else {
                NeeuralNetworkLearner.Timer.Stop();
                Model.Learn = false;
                button.Content = "learn";
            }
        }
        void ClearButtonClicked( object sender, RoutedEventArgs e ) {
            if ( !Model.Learn ) {
                Model.Era = 0;
                Model.Iteration = 0;
                Model.Error = 0;
                Model.NeuralNetworkErrorHistory.Clear();
                Model.NeuralNetwork.SetWeights();
                NeeuralNetworkLearner.Timer.Clear();
            }
        }
        void LearnWithErasChecked( object sender, RoutedEventArgs e ) {
            Model.LearningMode = Model.Learning.Mode.Eras;
        }
        void LearnWithErrorChecked( object sender, RoutedEventArgs e ) {
            Model.LearningMode = Model.Learning.Mode.Error;
        }
        void LearnWithAccurancyChecked( object sender, RoutedEventArgs e ) {
            Model.LearningMode = Model.Learning.Mode.Accurancy;
        }
        void NumberValidationTextBox( object sender, TextCompositionEventArgs e ) {
            Regex regex = new Regex(@"(\d)");
            e.Handled = !( e.Text == "," || regex.IsMatch(e.Text) );
        }
        async void SaveNeauralNetwork( object sender, RoutedEventArgs e ) {
            if ( !Model.Learn ) {
                await NeuralNetworkSaver.SaveAsync( Model.NeuralNetwork, Model.NeuralNetworkFilePath );
            }
        }
        async void SaveAsNeuralNetwork( object sender, RoutedEventArgs e ) {
            if ( !Model.Learn ) {

                SaveFileDialog saveFileDialog = new SaveFileDialog();

                saveFileDialog.Filter = "Neural network (*.nns)|*.nns";
                saveFileDialog.FileName = saveFileDialog.InitialDirectory + "MyNeuralNet.nns";
			    
                if( saveFileDialog.ShowDialog() == true ) {
				    await NeuralNetworkSaver.SaveAsync( Model.NeuralNetwork, saveFileDialog.FileName );
                    MessageBox.Show( "Neural network saved." );
                }
            }
        }
    }
    internal class Model {
        static internal class Learning {
            internal enum Mode {
                Eras,
                Error,
                Accurancy
            }
        }
        internal static string NeuralNetworkFileName = "no neuralnet file";
        internal static string DatasetFileName = "no dataset file";
        internal static string NeuralNetworkFilePath;
        internal static string DatasetFilePath;
        internal static string NeuralNetworkType;
        internal static int InputNeuronsCount;
        internal static int OutputNeuronsCount;
        internal static int DatasetFramesCount;
        internal static Learning.Mode LearningMode = Learning.Mode.Eras;
        internal static double LearningTarget;
        internal static bool Learn = false;
        internal static double LearningSpeed;
        internal static double Moment;
        internal static int Iteration;
        internal static int Era;
        internal static double Error;
        internal static double Accurancy;
        internal static List< double > NeuralNetworkErrorHistory = new List< double >();
        internal static NeuralNetwork NeuralNetwork;
        internal static Dataset Dataset;
        internal static string EraTime;
        internal static string IterationTime;
        internal static TimeSpan LearningTime = new TimeSpan();
        internal static iProgramData ProgramData = new iProgramData() ;
    }
    static class NeeuralNetworkLearner {
        public static async void StartLearn() {
            Model.Learn = true;

            switch ( Model.LearningMode ) {
                case Model.Learning.Mode.Eras:
                    await Task.Factory.StartNew( LearnWithEras );
                    break;

                case Model.Learning.Mode.Error:
                    await Task.Factory.StartNew( LearnWithError );
                    break;

                case Model.Learning.Mode.Accurancy:
                    await Task.Factory.StartNew( LearnWithAccurancy );
                    break;
            }
        }
        private static async Task LearnWithEras() {
            for ( int i = Model.Era; i < Model.LearningTarget; i++ ) {
                Stats_.error = 0.0;
                var eraStartTime = DateTime.Now;
                for ( int j = Model.Iteration; j < Model.DatasetFramesCount; j++ ) {
                    if ( Model.Learn ) {
                        var iterationStartTime = DateTime.Now;
                        
                        Model.Era = i;
                        Model.Iteration = j;
                        Model.NeuralNetwork.Work( Model.Dataset.Frames[j].InputValues );

                        Stats_.error += NeuralNetworkStatisticsCalculator.CalculateMSE(  Model.Dataset.Frames[j].IdealValues, Model.NeuralNetwork.OutputLayer.GetOutValues() );
                        Stats_.accurancy += NeuralNetworkStatisticsCalculator.CalculateMiddleAccurancy(  Model.Dataset.Frames[j].IdealValues, Model.NeuralNetwork.OutputLayer.GetOutValues() );
                        
                        Model.NeuralNetwork.Learn( Model.Dataset.Frames[j].IdealValues );
                        
                        var iterationDeltaTime = DateTime.Now - iterationStartTime;

                        Model.IterationTime = iterationDeltaTime.ToString();
                    }
                    else {
                        Model.Learn = false;
                        return;
                    }
                }
                Model.Iteration = 0;

                Model.Error = Stats_.error / Model.DatasetFramesCount;
                Model.Accurancy = Stats_.accurancy / Model.DatasetFramesCount;

                Model.NeuralNetworkErrorHistory.Add( Model.Error );

                var eraDeltaTime = DateTime.Now - eraStartTime;
                Model.EraTime = eraDeltaTime.ToString();
            }
            Model.Learn = false;
            await Task.Delay( 0 );
        }
        private static async Task LearnWithError() {
            var partCount = 1;
            for ( int i = Model.Era; ( Model.Error > Model.LearningTarget | Model.Era == 0 ); i++ ) {
                Stats_.error = 0.0;
                var eraStartTime = DateTime.Now;
                    
                for ( int j = Model.Iteration; j < Model.DatasetFramesCount; j += partCount ) {
                    if ( Model.Learn ) {
                        var iterationStartTime = DateTime.Now;
                        
                        Model.Era = i;
                        Model.Iteration = j;
                        Model.NeuralNetwork.Work( Model.Dataset.Frames[j].InputValues );

                        Stats_.error += NeuralNetworkStatisticsCalculator.CalculateMSE(  Model.Dataset.Frames[j].IdealValues, Model.NeuralNetwork.OutputLayer.GetOutValues() );
                        Stats_.accurancy += NeuralNetworkStatisticsCalculator.CalculateMiddleAccurancy(  Model.Dataset.Frames[j].IdealValues, Model.NeuralNetwork.OutputLayer.GetOutValues() );
                        
                        Model.NeuralNetwork.Learn( Model.Dataset.Frames[j].IdealValues );
                        
                        var iterationDeltaTime = DateTime.Now - iterationStartTime;

                        Model.IterationTime = iterationDeltaTime.ToString();
                    }
                    else {
                        Model.Learn = false;
                        return;
                    }
                }
                Model.Iteration = 0;

                Model.Error = Stats_.error / Model.DatasetFramesCount;
                Model.Accurancy = Stats_.accurancy / Model.DatasetFramesCount;

                Model.NeuralNetworkErrorHistory.Add( Model.Error );

                var eraDeltaTime = DateTime.Now - eraStartTime;
                Model.EraTime = eraDeltaTime.ToString();
                
            }
            Model.Learn = false;
            await Task.Delay( 0 );
        }
        private static async Task LearnWithAccurancy() {
            var partCount = 1;
            for ( int i = Model.Era; ( Model.Accurancy > Model.LearningTarget | Model.Era == 0 ); i++ ) {
                Stats_.error = 0.0;
                var eraStartTime = DateTime.Now;
                    
                for ( int j = Model.Iteration; j < Model.DatasetFramesCount; j += partCount ) {
                    if ( Model.Learn ) {
                        var iterationStartTime = DateTime.Now;
                        
                        Model.Era = i;
                        Model.Iteration = j;
                        Model.NeuralNetwork.Work( Model.Dataset.Frames[j].InputValues );

                        Stats_.error += NeuralNetworkStatisticsCalculator.CalculateMiddleAccurancy(  Model.Dataset.Frames[j].IdealValues, Model.NeuralNetwork.OutputLayer.GetOutValues() );
                        Stats_.accurancy += NeuralNetworkStatisticsCalculator.CalculateMiddleAccurancy(  Model.Dataset.Frames[j].IdealValues, Model.NeuralNetwork.OutputLayer.GetOutValues() );
                        
                        Model.NeuralNetwork.Learn( Model.Dataset.Frames[j].IdealValues );
                        
                        var iterationDeltaTime = DateTime.Now - iterationStartTime;

                        Model.IterationTime = iterationDeltaTime.ToString();
                    }
                    else {
                        Model.Learn = false;
                        return;
                    }
                }
                Model.Iteration = 0;

                Model.Error = Stats_.error / Model.DatasetFramesCount;
                Model.Accurancy = Stats_.accurancy / Model.DatasetFramesCount;

                Model.NeuralNetworkErrorHistory.Add( Model.Error );

                var eraDeltaTime = DateTime.Now - eraStartTime;
                Model.EraTime = eraDeltaTime.ToString();
                
            }
            Model.Learn = false;
            await Task.Delay( 0 );
        }
        private static class Stats_ {
            public static double error = 0.0;
            public static double accurancy = 0.0;
        }
        public static class Timer {
            private static bool count = false;
            public static void Start() {
                count = true;
                Task.Factory.StartNew( Count );
            }
            public static async void Count() {
                while ( count ) {
                    await Task.Delay( 1000 );
                    Model.LearningTime = Model.LearningTime.Add( TimeSpan.FromSeconds(1) );
                }
            }
            public static void Stop() {
                count = false;
            }
            public static void Clear() {
                count = false;
                Model.LearningTime = new TimeSpan();
            }
        }
    }
    class FileProcess {
        static public string GetFullName(string path) {
            path = NormalizePath(path);
            return path.Split('/')[path.Split('/').Length-1];
        }
        static public string GetName(string path) {
            path = NormalizePath(path);
            return path.Split('/')[path.Split('/').Length-1].Replace($".{GetType(path)}", "");
        }
        static public string GetType(string path) {
            path = NormalizePath(path);
            return path.Split('.')[path.Split('.').Length-1];
        }
        static public string GetPath(string path) {
            path = NormalizePath(path);
            string[] pathFragments = path.Split('/');
            path = "";
            for (int i = 0;i < pathFragments.Length-1;i++) {
                path += pathFragments[i] + "/";
            }
            path = path.Substring(0, path.Length-1);
            return path;
        }
        static public string NormalizePath(string path) {
            return path.Replace("\\", "/");
        }
    }
    abstract class Tools {
        public abstract class Chart {
            public static async Task DrawAsync( List< double > firstList, OxyPlot.Wpf.PlotView chart ) {
                var model = new PlotModel();
                var firstSeries = new OxyPlot.Series.LineSeries();

                firstSeries.StrokeThickness = 1;

                for ( int i = 0; i < firstList.Count; i++ ) {
                    firstSeries.Points.Add( new OxyPlot.DataPoint( i, firstList[i] ) );
                }

                model.Series.Add( firstSeries );

                chart.Model = model;
                await Task.Delay( 1 );
            }
            public static void Draw( List< double > firstList, OxyPlot.Wpf.PlotView chart ) {
                var model = new PlotModel();
                var firstSeries = new OxyPlot.Series.LineSeries();

                firstSeries.StrokeThickness = 1;

                for ( int i = 0; i < firstList.Count; i++ ) {
                    firstSeries.Points.Add( new OxyPlot.DataPoint( i, firstList[i] ) );
                }

                model.Series.Add( firstSeries );

                chart.Model = model;
            }
        }
    }
    [Serializable]
    class iProgramData {
        private string ProgramDataFilePath = @"data/progdata.dat";
        private string ProgramDataFileDirectory = @"data";
        public string RecentFilePath { get; set; }
        
        public iProgramData Get() {
            try {
                if ( !File.Exists( ProgramDataFilePath ) ) {
                    Directory.CreateDirectory( ProgramDataFileDirectory );
                    var file = File.Create( ProgramDataFilePath );
                    file.Close();
                    return this;
                }
                var stringData = File.ReadAllText( ProgramDataFilePath );
                if ( stringData.Length > 0 ) {
                    return JsonSerializer.Deserialize< iProgramData >( stringData );
                }
                return this;
            }
            catch {
                return this;
            }
        }
        public void Save() {
            using ( FileStream fs = new FileStream( ProgramDataFilePath, FileMode.OpenOrCreate ) ) {
                var serializedData = JsonSerializer.Serialize< iProgramData >( this );
                var bytes = Encoding.UTF8.GetBytes( serializedData );
                fs.Write( bytes );
            }
        }
    }
}
