using System;
[Serializable]
public class Material
{
	public static implicit operator bool(Material m)
	{
		return m == null;
	}
}
[Serializable]
public class SequenceOfNestedIfs
{
	public bool _clear;
	public Material _material;
	public override bool CheckShader()
	{
		return false;
	}
	public override void CreateMaterials()
	{
		if (!this._clear)
		{
			if (!this.CheckShader())
			{
				return;
			}
			this._material = new Material();
		}
		if (!this._material)
		{
			if (!this.CheckShader())
			{
				return;
			}
			this._material = new Material();
		}
		if (!this._material)
		{
			if (!this.CheckShader())
			{
				return;
			}
			this._material = new Material();
		}
		if (!this._material)
		{
			if (this.CheckShader())
			{
				this._material = new Material();
			}
		}
	}
}
