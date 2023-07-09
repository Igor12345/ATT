using SortingEngine.Entities;
using System.Diagnostics;

namespace SortingEngine;

public class PoolsManager
{
   private readonly int _bufferSize;
   private readonly LineMemory[]?[] _pool;
   private int _currentBuffer;
   private SpinLock _lock;

   public PoolsManager(int numberOfBuffers, int bufferSize)
   {
      _bufferSize = bufferSize;
      _pool = new LineMemory[numberOfBuffers][];
      _lock = new SpinLock(Debugger.IsAttached);
   }

   //System.Buffers.ConfigurableArrayPool.Bucket
   public LineMemory[] AcquireRecordsArray()
   {
      if (_currentBuffer >= _pool.Length)
         throw new InvalidOperationException("Attempt to acquire too many buffers");

      LineMemory[]?[] buffers = _pool;
      LineMemory[]? buffer = null;
      bool lockTaken = false, allocateBuffer = false;
      try
      {
         _lock.Enter(ref lockTaken);

         if (_currentBuffer < buffers.Length)
         {
            buffer = buffers[_currentBuffer];
            buffers[_currentBuffer++] = null;
            allocateBuffer = buffer == null;
         }
      }
      finally
      {
         if (lockTaken) _lock.Exit(false);
      }

      if (allocateBuffer)
      {
         buffer = new LineMemory[_bufferSize];
      }

      return buffer;
   }

   public void DeleteArrays()
   {
      bool lockTaken = false;
      try
      {
         _lock.Enter(ref lockTaken);
         Array.Clear(_pool);
      }
      finally
      {
         if (lockTaken) _lock.Exit(false);
      }
   }

   public void Return(LineMemory[] array)
   {
      bool lockTaken = false;
      try
      {
         _lock.Enter(ref lockTaken);
         
         if (_currentBuffer != 0)
         {
            _pool[--_currentBuffer] = array;
         }
      }
      finally
      {
         if (lockTaken) _lock.Exit(false);
      }
   }
}