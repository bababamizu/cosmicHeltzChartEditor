using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace chChartEditor
{
    public enum commandMode
    {
        Do,
        Undo,
        Redo
    }

    public class CommandManager
    {
        Stack<Command> undoStack = new Stack<Command>();
        Stack<Command> redoStack = new Stack<Command>();

        public CommandManager()
        {
            undoStack = new Stack<Command>();
            redoStack = new Stack<Command>();
        }

        /// <summary>
        /// 指定したコマンドを実行する
        /// </summary>
        public void Do(Command command)
        {
            undoStack.Push(command);
            command.Do();
            redoStack.Clear();
        }

        /// <summary>
        /// 操作を1つ元に戻す
        /// </summary>
        public void Undo()
        {
            if (undoStack.Count == 0)
                return;

            Command command = undoStack.Pop();
            redoStack.Push(command);
            command.Undo();
        }

        /// <summary>
        /// 元に戻した操作を1つやり直す
        /// </summary>
        public void Redo()
        {
            if (redoStack.Count == 0)
                return;

            Command command = redoStack.Pop();
            undoStack.Push(command);
            command.Redo();
            
        }

        public void ClearStack()
        {
            undoStack.Clear();
            redoStack.Clear();
        }

        public bool IsCanUndo()
        {
            return undoStack.Count > 0;
        }

        public bool IsCanRedo()
        {
            return redoStack.Count > 0;
        }
    }
}
    
