using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace chChartEditor
{
    public class Command
    {
        Action doAction;
        Action undoAction;
        Action redoAction;

        public Command(Action _do, Action _undo)
        {
            doAction = _do;
            undoAction = _undo;
            redoAction = _do;
        }

        public Command(Action _do, Action _undo, Action _redo)
        {
            doAction = _do;
            undoAction = _undo;
            redoAction = _redo;
        }

        public void Do()
        {
            doAction();
        }
        public void Undo()
        {
            undoAction();
        }
        public void Redo()
        {
            redoAction();
        }
    }
}

