﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace BotFileCreator
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Windows;
    using System.Windows.Input;
    using BotFileCreator.Repository;
    using Microsoft.Bot.Configuration;

    public class BotConfigurationViewModel : BaseViewModel
    {
        private readonly ICommand _cancelCommand;

        private readonly ICommand _createCommand;

        private readonly ICommand _botNameCommand;

        private readonly ICommand _botEndpointCommmand;

        private readonly ICommand _botServicesCommand;

        private readonly ICommand _botEncryptCommand;

        private readonly ICommand _isCheckedEncryptCheckBox;

        private readonly ICommand _copyCommand;

        private bool _encryptNoteIsVisible;

        private EndpointItem _endpointItem;

        private string _secretKey;

        private string _botFileName = string.Empty;

        private string _panelToShow = "BotName";

        public BotConfigurationViewModel()
        {
            _encryptNoteIsVisible = false;
            _copyCommand = new RelayCommand(param => this.CopySecretKey(), null);
            _isCheckedEncryptCheckBox = new RelayCommand(param => this.CheckEncryptCheckBox(), null);
            _createCommand = new RelayCommand(param => this.CreateBotFile(), null);
            _cancelCommand = new RelayCommand(param => this.CloseAction(), null);
            _botNameCommand = new RelayCommand(param => this.CollapsePanels("BotName"), null);
            _botEndpointCommmand = new RelayCommand(param => this.CollapsePanels("BotEndpoint"), null);
            _botServicesCommand = new RelayCommand(param => this.CollapsePanels("BotServices"), null);
            _botEncryptCommand = new RelayCommand(param => this.CollapsePanels("BotEncrypt"), null);
        }

        public Visibility EncryptNoteVisibility
        {
            get => EncryptNoteIsVisible ? Visibility.Visible : Visibility.Collapsed;
        }

        public bool EncryptNoteIsVisible
        {
            get => _encryptNoteIsVisible;
            set
            {
                _encryptNoteIsVisible = value;
                NotifyPropertyChanged("EncryptNoteVisibility");
            }
        }

        public string PanelToShow
        {
            get => _panelToShow;
            set
            {
                _panelToShow = value;
                NotifyPropertyChanged("BotNameVisibility");
                NotifyPropertyChanged("BotEndpointVisibility");
                NotifyPropertyChanged("BotServicesVisibility");
                NotifyPropertyChanged("BotEncrypVisibility");
            }
        }

        public Visibility BotNameVisibility
        {
            get => _panelToShow == "BotName" ? Visibility.Visible : Visibility.Collapsed;
        }

        public Visibility BotEndpointVisibility
        {
            get => _panelToShow == "BotEndpoint" ? Visibility.Visible : Visibility.Collapsed;
        }

        public Visibility BotServicesVisibility
        {
            get => _panelToShow == "BotServices" ? Visibility.Visible : Visibility.Collapsed;
        }

        public Visibility BotEncrypVisibility
        {
            get => _panelToShow == "BotEncrypt" ? Visibility.Visible : Visibility.Collapsed;
        }

        public string SecretKey { get => _secretKey; set => SetProperty(ref _secretKey, value); }

        public string BotFileName { get => _botFileName; set => SetProperty(ref _botFileName, value); }

        public EndpointItem EndpointItem
        {
            get
            {
                if (_endpointItem == null)
                {
                    _endpointItem = new EndpointItem();
                }

                return _endpointItem;
            }

            set => SetProperty(ref _endpointItem, value);
        }

        public bool EncryptCheckBoxIsChecked { get; set; }

        public ICommand CopyCommand { get => _copyCommand; }

        public ICommand CreateCommand { get => _createCommand; }

        public ICommand CancelCommand { get => _cancelCommand; }

        public ICommand BotNameCommand { get => _botNameCommand; }

        public ICommand BotEndpointCommand { get => _botEndpointCommmand; }

        public ICommand BotServicesCommand { get => _botServicesCommand; }

        public ICommand BotEncryptCommand { get => _botEncryptCommand; }

        public ICommand IsCheckedEncryptCheckBox { get => _isCheckedEncryptCheckBox; }

        public Action CloseAction { get; set; }

        public void CreateBotFile()
        {
            var botFileFullPath = GeneralSettings.Default.ProjectName;
            // BotFileNameManager botFileNameManager = new BotFileNameManager(BotFileName, botFileFullPath);

            // Checks if the bot configuration is valid
            Tuple<bool, string> configIsValid = BotFileConfigurationIsValid(BotFileName);

            // If the bot's configuration is not valid, it will show an error
            if (!configIsValid.Item1)
            {
                MessageBox.Show(configIsValid.Item2, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Repository for creating bot files
            IBotConfigurationRepository repository = new BotFileRepository(BotFileName, GetProjectDirectoryPath(botFileFullPath));

            // Adds the only endpoint (if it's not null) to the bot configuration
            if (!string.IsNullOrWhiteSpace(EndpointItem.Endpoint))
            {
                EndpointService endpoint = new EndpointService() { Name = this.EndpointItem.Name, Endpoint = this.EndpointItem.Endpoint, AppId = this.EndpointItem.AppId, AppPassword = this.EndpointItem.AppPassword };
                repository.ConnectService(endpoint);
            }

            // If the "encrypt" checkbox is checked, the bot configuration is save after encrypting it
            if (EncryptCheckBoxIsChecked)
            {
                repository.Save(this.SecretKey);
            }
            else
            {
                // Save the bot configuration without encryption
                repository.Save();
            }

            // Adds the just generated bot file to the project
            string filePath = Path.Combine(GetProjectDirectoryPath(botFileFullPath), BotFileName, ".bot");
            AddFileToProject(botFileFullPath, filePath);

            // If the file was successfully created, the Wizard will be closed.
            MessageBox.Show("Bot file successfully created", "Bot file successfully created", MessageBoxButton.OK, MessageBoxImage.Exclamation);

            if (!EncryptCheckBoxIsChecked)
            {
                CloseAction();
            }
        }

        private Tuple<bool, string> BotFileConfigurationIsValid(string botFileName)
        {
            // If the .bot file name is Null or WhiteSpace, returns an error.
            if (string.IsNullOrWhiteSpace(botFileName))
            {
                return new Tuple<bool, string>(false, "Bot file name can't be null.");
            }

            // If the .bot file name contains any whitespace, the method will return an error.
            if (botFileName.Contains(" "))
            {
                return new Tuple<bool, string>(false, "Bot file name can't have whitespaces.");
            }

            // A tuple with True and Empty string will be returned if there are no errors.
            return new Tuple<bool, string>(true, string.Empty);
        }

        /// <summary>
        /// Adds a specified file to another specified project
        /// </summary>
        /// <param name="projectName">The full path to .csproj file</param>
        /// <param name="fileName">The file name to add to csproj</param>
        private void AddFileToProject(string projectName, string fileName)
        {
            // Load a specific project. Also, avoids several problems for re-loading the same project more than once
            var project = Microsoft.Build.Evaluation.ProjectCollection.GlobalProjectCollection.LoadedProjects.FirstOrDefault(pr => pr.FullPath == projectName);

            if (project != null)
            {
                // Reevaluates the project to add any change
                project.ReevaluateIfNecessary();

                // Checks if the project has a file with the same name. If it doesn't, it will be added to the project
                if (project.Items.FirstOrDefault(item => item.EvaluatedInclude == fileName) == null)
                {
                    project.AddItem("Compile", fileName);
                    project.Save();
                }
            }
        }

        /// <summary>
        /// Returns the Working Project Directory
        /// </summary>
        /// <param name="projectPath">Project's full path</param>
        /// <returns>Project's directory path</returns>
        private string GetProjectDirectoryPath(string projectPath)
        {
            return projectPath.Substring(0, projectPath.LastIndexOf('\\'));
        }

        private void CollapsePanels(string panelToShow)
        {
            this.PanelToShow = panelToShow;
        }

        private void CloseWindow(Window window)
        {
            if (window != null)
            {
                window.Close();
            }
        }

        private void CheckEncryptCheckBox()
        {
            EncryptCheckBoxIsChecked = !EncryptCheckBoxIsChecked;
            EncryptNoteIsVisible = !EncryptNoteIsVisible;
            this.SecretKey = BotFileRepository.GenerateKey();
        }

        private void CopySecretKey()
        {
            if (!string.IsNullOrWhiteSpace(SecretKey))
            {
                Clipboard.SetText(SecretKey);
            }
        }
    }
}
