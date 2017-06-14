/*
 * Copyright (C) 2017 Bartosz Meglicki <meglickib@gmail.com>
 *
 * This program is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License version 3 as
 * published by the Free Software Foundation.
 * This program is distributed "as is" WITHOUT ANY WARRANTY of any
 * kind, whether express or implied; without even the implied warranty
 * of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 */

using System;
using System.Collections;
using System.Collections.Generic;
	
/// <summary>
/// DBSCAN data wrapper for cluster without copying the actual data 
/// </summary>
public class DBSCANCluster : IList<ScanPoint>, IList
{
	public int LowerIndex {get;private set;}
	public int UpperIndex {get;private set;}
	public int Id { get;private set; } 

	public DBSCANCluster(IList<ScanPoint> data, int lower, int upper, int id)
	{
		this.data = data;
		LowerIndex = lower;
		UpperIndex = upper;
		Id = id;
	}

	private IList<ScanPoint> data; 

	#region Implementation of IEnumerable

	public IEnumerator<ScanPoint> GetEnumerator()
	{
		return new ScanPointEnumerator(this);
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return new ScanPointEnumerator(this);
	}

	#endregion

	#region Implementation of ICollection<T>

	public void Add(ScanPoint item)
	{
		throw new System.InvalidOperationException("Cluster is fixed size IList, Add not allowed");
	}

	public void Clear()
	{
		throw new System.InvalidOperationException("Cluster is fixed size IList, Clear not allowed");
	}

	public bool Contains(ScanPoint item)
	{
		throw new System.NotImplementedException();
	}

	public void CopyTo(ScanPoint[] array, int arrayIndex)
	{
		throw new System.NotImplementedException();
	}

	public bool Remove(ScanPoint item)
	{
		throw new System.InvalidOperationException("Cluster is fixed size IList, Remove not allowed");
	}

	public int Count
	{
		get { return UpperIndex - LowerIndex + 1; }
	}

	public bool IsReadOnly
	{
		get { return false; }
	}

	#endregion

	#region Implementation of IList<T>

	public int IndexOf(ScanPoint item)
	{
		throw new System.NotImplementedException();
	}

	public void Insert(int index, ScanPoint item)
	{
		throw new System.InvalidOperationException("Cluster is fixed size IList, Insert not allowed");
	}

	public void RemoveAt(int index)
	{
		throw new System.InvalidOperationException("Cluster is fixed size IList, RemoveAt not allowed");
	}

	public ScanPoint this[int index]
	{
		get { return data[Mod(LowerIndex+index, data.Count)]; }
		set { data[Mod(LowerIndex+index, data.Count)] = value; }
	}

	#endregion

	#region IList implementation

	public int Add(object value)
	{
		throw new NotImplementedException();
	}
		
	public bool Contains(object value)
	{
		throw new NotImplementedException();
	}

	public int IndexOf(object value)
	{
		throw new NotImplementedException();
	}

	public void Insert(int index, object value)
	{
		throw new NotImplementedException();
	}

	public bool IsFixedSize
	{
		get { return true; }
	}
		
	public void Remove(object value)
	{
		throw new NotImplementedException();
	}
		
	object IList.this[int index]
	{
		get { return this[index]; }
		set { this[index] = (ScanPoint)value; }
	}

	public void CopyTo(Array array, int index)
	{
		throw new NotImplementedException();
	}
		
	public bool IsSynchronized
	{
		get { return false;}
	}

	public object SyncRoot
	{
		get { throw new NotImplementedException(); }
	}
		
	#endregion

	private int Mod(int i, int m)
	{
		int r = i % m;
		return (r < 0) ? r+m : r;
	}

	public class ScanPointEnumerator : IEnumerator<ScanPoint>
	{
		private DBSCANCluster cluster;
		private int index;
		private ScanPoint point;
	
		public ScanPointEnumerator(DBSCANCluster cluster)
		{
			this.cluster = cluster;
			index = -1;
			point = default(ScanPoint);

		}

		public bool MoveNext()
		{
			if (++index >= cluster.Count)
				return false;
	
			point = cluster[index];

			return true;
		}

		public void Reset() { index = -1; }

		void IDisposable.Dispose() { }

		public ScanPoint Current
		{
			get { return point; }
		}
			
		object IEnumerator.Current
		{
			get { return Current; }
		}
	}
}
