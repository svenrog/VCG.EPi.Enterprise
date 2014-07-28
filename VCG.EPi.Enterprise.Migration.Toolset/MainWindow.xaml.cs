using System;
using System.IO;
using System.IO.Compression;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Resources;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Runtime.Serialization.Formatters.Binary;

using VCG.EPi.Enterprise.IO;
using VCG.EPi.Enterprise.Xml.Transforms;
using VCG.EPi.Enterprise.Logging;
using VCG.EPi.Enterprise.Migration.Toolset.Data;
using VCG.EPi.Enterprise.Migration.Toolset.Types;
using VCG.EPi.Enterprise.Migration.Toolset.Xml;
using VCG.EPi.Enterprise.Migration.Toolset.Xml.Transforms;
using VCG.EPi.Enterprise.Migration.Toolset.Serialization;
using VCG.EPi.Enterprise.Migration.Toolset.Properties;

using ProtoBuf;
using ProtoBuf.Meta;

using Microsoft.Win32;
using MahApps.Metro.Controls;

using Shell = System.Windows.Shell;
using Forms = System.Windows.Forms;

namespace VCG.EPi.Enterprise.Migration.Toolset
{
	public partial class MainWindow : MetroWindow
	{
		private readonly string _allowedFileType = ".episerverdata";
		
		public Document CurrentDocument = new Document();
        public ExportResultWindow CurrentExport;

		public MainWindow()
		{
            InitializeLanguage();
			InitializeComponent();
			BindEvents();

			// Prepare serialization
			ProtocolSerializationManager.PrepareSerializer();
		}

		#region Properties

		private CultureInfo _currentCulture;
		public CultureInfo CurrentCulture
		{
			get
			{
				if (_currentCulture == null)
					_currentCulture = new CultureInfo(Settings.Default.Language);

				return _currentCulture;
			}

			set
			{
				Settings.Default.Language = value.ToString();
				Thread.CurrentThread.CurrentUICulture = value;
                Thread.CurrentThread.CurrentCulture = value;
				_currentCulture = value;
			}
		}

        private List<CultureInfo> _availableCultures;

        public List<CultureInfo> AvailableCultures
        {
            get
            {
                if (_availableCultures == null)
                {
                    _availableCultures = new List<CultureInfo>();

                    string location = typeof(MainWindow).Assembly.Location;
                    string executablePath = Path.GetDirectoryName(location);
                    string[] directories = Directory.GetDirectories(executablePath);

                    foreach (string directory in directories)
                    {
                        try
                        {
                            DirectoryInfo language = new DirectoryInfo(directory);
                            _availableCultures.Add(new CultureInfo(language.Name));
                        }
                        catch (Exception) {}
                    }
                }

                return _availableCultures;
            }
        }

		#endregion

		#region Menu Methods

		private void Application_Closing(object sender, EventArgs args)
		{
			
		}

		private void Menu_New_Click(object sender, RoutedEventArgs e)
		{
			CurrentDocument = new Document();

			SourcePageTypes.ItemsSource = null;
			TargetPageTypes.ItemsSource = null;
		}

		private void Menu_Open_Click(object sender, RoutedEventArgs e)
		{
			OpenFileDialog dialog = new OpenFileDialog()
			{
				Filter = Properties.Resources.DocumentFilters,
				FilterIndex = 1,
				Multiselect = false
			};

			bool? result = dialog.ShowDialog();

			if (result == true)
			{
                try
                {
					SetStatus(Properties.Resources.DocumentOpening);

					StartThread(() => 
					{
						var loadedDocument = new Document();
						loadedDocument.Open(dialog.FileName);
					});
                }
                catch(Exception ex)
                {
                    SetStatus(string.Format(Properties.Resources.ErrorFormat, ex.Message));
                }
			}
		}

        private void Menu_Preferences_Click(object sender, RoutedEventArgs e)
        {
            var preferences = new Preferences();
            preferences.ShowDialog();
        }

		private void Menu_Import_Source_Click(object sender, RoutedEventArgs e)
		{
			OpenFileDialog dialog = new OpenFileDialog()
			{
				Title = Properties.Resources.ImportSourceTitle,
				Filter = Properties.Resources.ImportFilters,
				FilterIndex = 1,
				Multiselect = false
			};

			bool? result = dialog.ShowDialog();

			if (result == true)
			{
				ImportSource(dialog.FileName);
			}
		}

		private void Menu_Import_Target_Click(object sender, RoutedEventArgs e)
		{
			OpenFileDialog dialog = new OpenFileDialog()
			{
				Title = Properties.Resources.ImportTargetTitle,
				Filter = Properties.Resources.ImportFilters,
				FilterIndex = 1,
				Multiselect = false
			};

			bool? result = dialog.ShowDialog();

			if (result == true)
			{
				ImportTarget(dialog.FileName);
			}
		}

		private void Menu_Save_Click(object sender, RoutedEventArgs e)
		{
			if (CurrentDocument.DocumentPath == null)
				Menu_SaveAs_Click(sender, e);

			CurrentDocument.Save();
			SetStatus(Properties.Resources.DocumentSaved);

            try
            {
                
            }
            catch (Exception ex)
            {
                SetStatus(string.Format(Properties.Resources.ErrorFormat, ex.Message));
            }
		}

		private void Menu_SaveAs_Click(object sender, RoutedEventArgs e)
		{
			SaveFileDialog dialog = new SaveFileDialog()
			{
				FileName = Properties.Resources.DocumentSaveFileName,
				DefaultExt = ".mgproj",
				Filter = Properties.Resources.DocumentSaveFilter
			};

			bool? result = dialog.ShowDialog();

			if (result == true)
			{
				string path = dialog.FileName;

				CurrentDocument.SaveAs(path);
				SetStatus(Properties.Resources.DocumentSaved);

                try
                {
                  
                }
                catch (Exception ex)
                {
                    SetStatus(string.Format(Properties.Resources.ErrorFormat, ex.Message));
                }				
			}
		}

		private void Menu_Export_to_File_Click(object sender, RoutedEventArgs e)
		{
			if (CurrentDocument.SourceMappings == null)
			{
				SetStatus(Properties.Resources.ExportAbortedNoData);
				return;
			}

            bool upgrade = CurrentDocument.SourceMappings.Any(p => p.Target != null);

			if (upgrade)
			{
				SaveFileDialog dialog = new SaveFileDialog()
				{
					FileName = Properties.Resources.ExportSaveFileName,
					DefaultExt = _allowedFileType,
					Filter = Properties.Resources.ExportSaveFilter
				};

				bool? result = dialog.ShowDialog();

				if (result == true)
				{
					SetStatus(Properties.Resources.ExportProgress);

                    ExportToFile(dialog.FileName);
				}
				else
				{
					SetStatus(Properties.Resources.ExportAbortedUser);
				}
			}
			else
			{
				SetStatus(Properties.Resources.ExportAbortedNoMatches);
			}
		}

		private void Menu_Generate_Classes_Click(object sender, RoutedEventArgs e)
		{
			if (CurrentDocument.SourceMappings == null)
			{
				SetStatus(Properties.Resources.ExportAbortedNoData);
				return;
			}

			ClassExportOptions exportOptions = new ClassExportOptions();
			bool? exportOptionsResult = exportOptions.ShowDialog();

			if (exportOptionsResult == true)
			{
				Forms.FolderBrowserDialog dialog = new Forms.FolderBrowserDialog()
				{
					Description = Properties.Resources.ExportSelectFolder
				};

				Forms.DialogResult fbresult = dialog.ShowDialog();

				if (fbresult == Forms.DialogResult.OK)
				{
                    string path = dialog.SelectedPath.TrimEnd('\\');
                    ExportClasses(path, exportOptions.Options);
				}
				else
				{
					SetStatus(Properties.Resources.ExportAbortedUser);
				}
			}
			else
			{
				SetStatus(Properties.Resources.ExportAbortedUser);
			}
		}

		#endregion

		#region Generic Events

		private void SourceTypes_DoubleClick(object sender, RoutedEventArgs e)
		{
			XmlTargetMapping mapping = (XmlTargetMapping)((ListView)sender).SelectedItem;

			PropertyAliasDefinitions definitions = new PropertyAliasDefinitions(mapping);
			definitions.ShowDialog();

            MatchingProperties.ItemsSource = definitions.Mapping.MatchingProperties;
		}

		private void Generic_DragOver(object sender, DragEventArgs e)
		{
			bool dropEnabled = true;

			if (e.Data.GetDataPresent(DataFormats.FileDrop, true))
			{
				string[] filenames = e.Data.GetData(DataFormats.FileDrop, true) as string[];

				foreach (string filename in filenames)
				{
					if (Path.GetExtension(filename).ToLower() != _allowedFileType)
					{
						dropEnabled = false;
						break;
					}
				}
			}
			else
			{
				dropEnabled = false;
			}

			if (!dropEnabled)
			{
				e.Effects = DragDropEffects.None;
				e.Handled = true;
			}
		}

        private void Upgrade_OnUpgradeComplete(object sender, EventArgs e)
        {
            if (TaskbarItemInfo != null)
            {
                TaskbarItemInfo.ProgressState = Shell.TaskbarItemProgressState.Indeterminate;
            }

            CurrentExport.btnOk.IsEnabled = true;
            CurrentExport.pbProgress.Value = 100;
            CurrentExport.ShowCloseButton = true;

            SetStatus(Properties.Resources.UpgradeComplete);
        }

        private void Upgrade_OnUpgradeProgress(object sender, ProgressLogEventArgs e)
        {
            if (TaskbarItemInfo != null)
            {
                TaskbarItemInfo.ProgressValue = e.ProgressPercentage;
            }

            CurrentExport.tbLog.AppendText(e.MessageStack);
            CurrentExport.tbLog.ScrollToEnd();

            CurrentExport.pbProgress.Value = e.ProgressPercentage;

			SetStatus(string.Format(Properties.Resources.UpgradeProgress, e.ProgressPercentage));
        }

        private void SourcePageTypes_SizeChanged(object sender, SizeChangedEventArgs e)
        {
			// ListViews does not support autosizing headers.
			// This method takes care of that.

            ListView listView = sender as ListView;
            GridView gView = listView.View as GridView;

            var workingWidth = listView.ActualWidth - 14 - 30 - 60;
            var col2 = 0.50;
            var col4 = 0.50;

            gView.Columns[0].Width = 30;
            gView.Columns[1].Width = workingWidth * col2;
            gView.Columns[2].Width = 60;
            gView.Columns[3].Width = workingWidth * col4;
        }

		private void Document_OnDocumentNotFound(object sender, FileNotFoundRetryEventArgs e)
		{
			// Current work document does not save the entire contents of .episerverdata-files.
			// When project files are opened on different computers or if project is moved,
			// the saved paths inside the project file will be invalid
			// This method provides a way for the user to find the file again

			// Todo: implement relative path handling in document.

			string initialDirectory = null;
			string supposedFileDirectory = System.IO.Path.GetDirectoryName(e.FileName);

			if (Directory.Exists(supposedFileDirectory))
				initialDirectory = supposedFileDirectory;

			OpenFileDialog dialog = new OpenFileDialog()
			{
				Title = string.Format("Where is {0}?", e.FileName),
				InitialDirectory = initialDirectory,
				Filter = "EPiServer data (.episerverdata)|*.episerverdata",
				FilterIndex = 1,
				Multiselect = false
			};

			bool? result = dialog.ShowDialog();

			if (result == true)
				e.RetryReference(dialog.FileName);
		}

		private void Document_OnDocumentLoaded(object sender, DocumentLoadedEventArgs args)
		{
			CurrentDocument = args.Document;

			SourcePageTypes.ItemsSource = CurrentDocument.SourceMappings;
			TargetPageTypes.ItemsSource = CurrentDocument.Targets;

			SetStatus(Properties.Resources.DocumentLoaded);
		}

		private void Document_OnDocumentLoading(object sender, ProgressStreamReportEventArgs args)
		{
			double percent = ((double)args.StreamPosition / (double)args.StreamLength) * 100;

			SetStatus(string.Format(CultureInfo.InvariantCulture, Properties.Resources.DocumentLoading, percent));
		}

		private void Document_OnFileImported(object sender, FileImportedEventArgs args)
		{
			SetStatus(Properties.Resources.FileImported);
		}

		private void Document_OnError(object sender, ErrorEventArgs args)
		{
			SetStatus(string.Format(Properties.Resources.ErrorFormat, args.GetException().Message));
		}

		#endregion

		#region SourcePageTypes

		private void SourcePageTypes_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			XmlTargetMapping mapping = e.AddedItems.Count > 0 ? (XmlTargetMapping)e.AddedItems[0] : null;

			if (mapping != null)
			{
				TargetPageTypes.SelectedItem = mapping.Target;
				MatchingProperties.ItemsSource = mapping.MatchingProperties;
			}
		}

		private void SourcePageTypes_Drop(object sender, DragEventArgs e)
		{
			if (e.Data.GetDataPresent(DataFormats.FileDrop))
			{
				string[] files = e.Data.GetData(DataFormats.FileDrop) as string[];
				string file = files.First();

				ImportSource(file);
			}
		}

		private void ImportSource(string path)
		{
			StartThread(() =>
			{
				if (CurrentDocument.LoadSource(path, true))
				{
					CurrentDocument.ReadSourcePageTypes();

					Dispatcher.Invoke(() =>
					{
						SourcePageTypes.ItemsSource = CurrentDocument.SourceMappings;
					});
				}
			});
		}

		#endregion

		#region TargetPageTypes

		private void TargetPageTypes_Drop(object sender, DragEventArgs e)
		{
			if (e.Data.GetDataPresent(DataFormats.FileDrop))
			{
				string[] files = e.Data.GetData(DataFormats.FileDrop) as string[];
				string file = files.First();

				ImportTarget(file);
			}
		}

		private void TargetPageTypes_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (SourcePageTypes.SelectedItem != null)
			{
				XmlTargetMapping mapping = (XmlTargetMapping)SourcePageTypes.SelectedItem;
				XmlTarget target = e.AddedItems.Count > 0 ? (XmlTarget)e.AddedItems[0] : null;

				if (target != mapping.Target)
				{
					mapping.Target = target;
				}

				MatchingProperties.ItemsSource = mapping.MatchingProperties;

				if (mapping.Target != null)
				{
					if (mapping.MatchingProperties != null)
					{
						MatchingProperties.IsEnabled = true;
						return;
					}
				}
			}

			MatchingProperties.ItemsSource = null;
			MatchingProperties.IsEnabled = false;
		}

		private void ImportTarget(string path)
		{
			StartThread(() =>
			{
				if (CurrentDocument.LoadTarget(path, true))
				{
					CurrentDocument.ReadTargetPageTypes();

					Dispatcher.Invoke(() => 
					{ 
						TargetPageTypes.ItemsSource = CurrentDocument.Targets;

						if (CurrentDocument.SourceMappings != null)
						{
							foreach (XmlTargetMapping mapping in CurrentDocument.SourceMappings)
							{
								mapping.Target = null;
							}
						}
					});
				}
			});
		}

		#endregion

        #region Methods

		public void InitializeLanguage()
		{
			Thread.CurrentThread.CurrentUICulture = CurrentCulture;
		}

		public void BindEvents()
		{
			Document.Operation = AsyncOperationManager.CreateOperation(null);
			Document.OnDocumentNotFound += Document_OnDocumentNotFound;
			Document.OnDocumentLoaded += Document_OnDocumentLoaded;
			Document.OnDocumentLoadingProgress += Document_OnDocumentLoading;
			Document.OnFileImported += Document_OnFileImported;
			Document.OnError += Document_OnError;

			SourcePageTypes.AddHandler(MouseDoubleClickEvent, new RoutedEventHandler(SourceTypes_DoubleClick));
		}

        private void StartThread(Action action)
        {
            var start = new ThreadStart(action);
            var thread = new Thread(start);
            thread.IsBackground = true;
            thread.Priority = ThreadPriority.Lowest;
            thread.Start();
        }

        public void SetStatus(string status) { lblStatus.Text = status; }

        private void ExportClasses(string path, ClassGenerationOptions options)
        {
            string completePath = path + "\\Models";

            var log = new LogBase();

            try
            {
                if (!Directory.Exists(completePath + "\\Pages"))
                    Directory.CreateDirectory(completePath + "\\Pages");

                if (!Directory.Exists(completePath + "\\Blocks"))
                    Directory.CreateDirectory(completePath + "\\Blocks");

                HashSet<string> files = new HashSet<string>();

                StartThread(() =>
                {
                    int count = 0;
                    int mappings = CurrentDocument.SourceMappings.Count;

                    foreach (var mapping in CurrentDocument.SourceMappings)
                    {
                        string className = EPiServerClassGenerator.ClassNameFromTargetMapping(mapping);
                        string fileName = className + ".cs";
                        string typePath = mapping.ExportAsBlock ? "Blocks" : "Pages";
                        string filePath = completePath + "\\" + typePath + "\\" + fileName;

                        log.Log(string.Format(Properties.Resources.LogGeneratingClassFormat, className));

                        string classContents = EPiServerClassGenerator.ClassFromTargetMapping(mapping, CurrentDocument.SourceFile, options, log);

                        count++;

                        if (files.Contains(filePath))
                        {
                            lock (log)
                            {
								log.Log(string.Format(Properties.Resources.LogClassConflictFormat, fileName), MessageType.Warning);
                            }

                            continue;
                        }

                        using (StreamWriter file = new StreamWriter(filePath, false, Encoding.UTF8, 16 * 1024))
                        {
                            file.Write(classContents);
                            files.Add(filePath);
                        }

						// UI thread needs to be invoked from running thread
						// This is a hack yao.
                        Dispatcher.Invoke(() =>
                        {
                            CurrentExport.tbLog.AppendText(log.GetLogChanges());
                            CurrentExport.tbLog.ScrollToEnd();

                            CurrentExport.pbProgress.Value = ((double)count / (double)mappings) * 100;
                        });
                    }

					// This is also a hack
					// Try to place it outside the thread call
					// See what happens
                    Dispatcher.Invoke(() =>
                    {
						SetStatus(Properties.Resources.ClassesGenerated);
                        CurrentExport.btnOk.IsEnabled = true;
                    });

                });

                CurrentExport = new ExportResultWindow(null);
                CurrentExport.ShowDialog();

            }
            catch (Exception ex)
            {
                log.Log(ex.Message + "\r\n" + ex.StackTrace.ToString(), MessageType.Error);
                SetStatus(string.Format(Properties.Resources.ErrorFormat, ex.Message));
            }
        }

        private void ExportToFile(string path)
        {
            var log = new LogBase();

            var upgradePages = CurrentDocument.SourceMappings.Where(p => p.Target != null).ToList();
            var removePages = CurrentDocument.SourceMappings.Where(p => p.Target == null).ToList();

            List<IXPathTransform> transforms = new List<IXPathTransform>();

            // Move all converted blocks to separate node and set ParentLink to global block folder
            // The moving of blocks is to mimic the structure of EPiServer 7 export data
            if (upgradePages.Any(p => p.Target.Type == ContentType.Block))
            {
                // Fortunately Global Assets Folder always has the same PageGUID.
                Guid contentFolderGuid = Guid.Parse("E56F85D0-E833-4E02-976A-2D11FE4D598C");

                // Adding content proved hard (do more tests later), had problems relating to PermanentLinkMap
                // For now just dump the blocks inside Global Assets Folder (further down)
                //transforms.Add(new AddBlockFolderTransform(contentFolderGuid, log));

                var blockPages = upgradePages.Where(p => p.Target.Type == ContentType.Block);

                foreach (var page in blockPages)
                {
                    transforms.Add(new ContentParentTransform(page.Source.Id, contentFolderGuid, log));
                }
            }

            foreach (var removePage in removePages)
                transforms.Add(new PageTypeRemovalTransform(removePage.Source.Id, log));

            foreach (var upgradePage in upgradePages)
                transforms.Add(new PageTypeUpgradeTransform(upgradePage, log));

            // Remove all remaining <pagetypes>
            // Tests indicated that there is a possibility of stragglers
            // if the export file is generated with "auto export page types".
            transforms.Add(new PageTypesRemovalTransform(log));

            // Hold on te yer breeches, here there be pirates!

            //						| |
            //			            | |
            //					    | |
            //					  \     /
            //					   \   /
            //					    \ /

            try
            {
                // First make a copy of the file to destination
                File.Copy(CurrentDocument.SourceAbsolutePath, path, true);

                var operation = AsyncOperationManager.CreateOperation(null);
                var upgrade = new EPiDataUpgradeWorker(operation);

                // Hookup events to monitor progress
                upgrade.OnUpgradeProgress += Upgrade_OnUpgradeProgress;
                upgrade.OnUpgradeComplete += Upgrade_OnUpgradeComplete;

                if (TaskbarItemInfo != null)
                    TaskbarItemInfo.ProgressState = Shell.TaskbarItemProgressState.Normal;

                // Then run the rewriting of the file on a different thread
                StartThread(() => 
				{ 
					upgrade.UpgradeDataPackage(path, transforms, log);
				});

                // Display the progress
                CurrentExport = new ExportResultWindow(null);
                CurrentExport.ShowDialog();

            }
            catch (Exception ex)
            {
                log.Log(ex.Message + "\r\n" + ex.StackTrace.ToString(), MessageType.Error);

                SetStatus(string.Format(Properties.Resources.ErrorFormat, ex.Message));
            }
        }

        #endregion

       

    }
}
