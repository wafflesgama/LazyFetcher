using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace LazyFetcher
{
    public class DialogueWindow : EditorWindow
    {

        public static (bool, string) DisplayDialogue(string title, string message, string ok, string cancel, string defaultLabel)
        {
            var window = GetWindow<DialogueWindow>(title);
            window.SetupInfo(title, message, ok, cancel, defaultLabel);
            window.ShowModalUtility();
            return (window.sucess, window.GetInputValue());
        }

        private VisualElement _root;

        //public delegate void Notify(bool success, string inputValue);
        //public event Notify OnClose;

        public bool sucess;

        private Label _message;
        private Button _okBttn;
        private Button _cancelBttn;
        private TextField _inputField;

        private string inputPlaceHolder;
        //string message;
        //string ok;
        //string cancel;

        //public void Init(string title, string message, string ok, string cancel, string defaultLabel)
        //{
        //    titleContent.text = title;
        //    this.message = message;
        //    this.ok = ok;
        //    this.cancel = cancel;
        //}

        public void SetupInfo(string title, string message, string ok, string cancel, string defaultLabel)
        {
            titleContent.text = title;
            _message.text = message;
            _okBttn.text = ok;
            _cancelBttn.text = cancel;

            this.inputPlaceHolder = defaultLabel;
            _inputField.value = inputPlaceHolder;
        }
        private void OnEnable()
        {
            SetupBaseUI();
            SetupBindings();
            //SetupInfo();
            SetupCallbacks();

        }

        private void OnDestroy()
        {
            //if (this.OnClose != null)
            //    OnClose.Invoke(sucess, GetInputValue());
        }



        private void SetupBaseUI()
        {
            _root = rootVisualElement;

            // Loads and clones our VisualTree (eg. our UXML structure) inside the root.
            var quickToolVisualTree = (VisualTreeAsset)AssetDatabase.LoadAssetAtPath(PathFactory.BuildUiFilePath(PathFactory.DIALOGUE_LAYOUT_FILE), typeof(VisualTreeAsset));
            quickToolVisualTree.CloneTree(_root);

        }

        private void SetupBindings()
        {

            _message = (Label)_root.Q("Message");
            _inputField = (TextField)_root.Q("InputField");
            _okBttn = (Button)_root.Q("OkBttn");
            _cancelBttn = (Button)_root.Q("CancelBttn");
        }



        private void SetupCallbacks()
        {
            _okBttn.RegisterCallback<ClickEvent>((x) => OkClicked());
            _cancelBttn.RegisterCallback<ClickEvent>((x) => this.Close());
        }


        private string GetInputValue()
        {

            if (inputPlaceHolder == _inputField.value)
                return string.Empty;

            return _inputField.value;
        }

        private void OkClicked()
        {
            sucess = true;

            this.Close();
        }
    }
}
