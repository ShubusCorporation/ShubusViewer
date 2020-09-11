using System;
using System.Collections.Generic;
using System.Windows.Forms;
using StateMachine;
using ExtendedData;
using System.Reflection;
using ShubusViewer.Components;

namespace ShubusViewer
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            AppController myController = new AppController();
            Form1 myForm = new Form1(myController, new TextProcessor());

            if (args.GetLength(0) > 0)
            {
                if (args.Length > 1 && args[1].Length > 0)
                {
                    if (args[0] == Constants.STR_ARG_TEXTMODE)
                    {
                        myController.setFileName(args[1]);
                        myForm.Load += (a, b) => myController.processOperation(TypeAction.ETextForce);    
                    }
                    else if (args[1] == Constants.STR_ARG_TEXTMODE)
                    {
                        myController.setFileName(args[0]);
                        myForm.Load += (a, b) => myController.processOperation(TypeAction.ETextForce);
                    }
                    else
                    {
                        myController.setFileName(args[0]);
                        myForm.Load += (a, b) => myController.processOperation(TypeAction.EOpen);
                    }
                }
                else
                {
                    myController.setFileName(args[0]);
                    myForm.Load += (a, b) => myController.processOperation(TypeAction.EOpen);
                }                
            }
            else
            {
                myController.setFileName(Constants.STR_DEFAULT_FN);
            }
            try { Application.Run(myForm); }
            catch (Exception e)
            {
                MessageBox.Show(e.StackTrace);
            }
        }
    }
}