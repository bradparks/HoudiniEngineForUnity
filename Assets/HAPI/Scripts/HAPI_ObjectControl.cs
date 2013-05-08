/*
 * PROPRIETARY INFORMATION.  This software is proprietary to
 * Side Effects Software Inc., and is not to be reproduced,
 * transmitted, or disclosed in any way without written permission.
 *
 * Produced by:
 *      Side Effects Software Inc
 *		123 Front Street West, Suite 1401
 *		Toronto, Ontario
 *		Canada   M5J 2M2
 *		416-504-9876
 *
 * COMMENTS:
 * 
 */

using UnityEngine;
using UnityEditor;
using System.Collections;

using HAPI;

public class HAPI_ObjectControl : HAPI_Control 
{

	/////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	// Public Properties
	
	// Please keep these in the same order and grouping as their initializations in HAPI_Control.reset().

	public int		prObjectId {		get { return myObjectId; }			set { myObjectId = value; } }
	public string	prObjectName {		get { return myObjectName; }		set { myObjectName = value; } }
	public bool		prObjectVisible {	get { return myObjectVisible; }		set { myObjectVisible = value; } }

	public HAPI_ObjectControl() 
	{
		reset();
	}

	~HAPI_ObjectControl()
	{

	}

	public override void reset()
	{
		base.reset();

		// Please keep these in the same order and grouping as their declarations at the top.
		
		myObjectId		= -1;
		myObjectName	= "object_name";
		myObjectVisible	= false;
	}

	public void init( HAPI_ObjectControl object_control )
	{
		prAssetId		= object_control.prAssetId;
		prObjectId		= object_control.prObjectId;
		prObjectName	= object_control.prObjectName;
		prObjectVisible = object_control.prObjectVisible;
	}
	public void init( int asset_id, int object_id, string object_name, bool object_visible )
	{
		prAssetId		= asset_id;
		prObjectId		= object_id;
		prObjectName	= object_name;
		prObjectVisible = object_visible;
	}
	
	private void addKeyToCurve( float time, float val, AnimationCurve curve )
	{
		Keyframe curr_key = new Keyframe( time, val, 0, 0 );						
		curve.AddKey( curr_key );		
	}
	
	
	public void beginBakeAnimation()
	{
		myCurveCollection = new HAPI_CurvesCollection();					
	}
	
		
	public void bakeAnimation(  float curr_time, GameObject parent_object, HAPI_Transform hapi_transform )
	{
		try
		{
						
			Matrix4x4 parent_xform_inverse = Matrix4x4.identity;
				
			if( parent_object != null )
				parent_xform_inverse = parent_object.transform.localToWorldMatrix.inverse;
			
													
										
			Vector3 pos = new Vector3();
			
			// Apply object transforms.
			//
			// Axis and Rotation conversions:
			// Note that Houdini's X axis points in the opposite direction that Unity's does.  Also, Houdini's 
			// rotation is right handed, whereas Unity is left handed.  To account for this, we need to invert
			// the x coordinate of the translation, and do the same for the rotations (except for the x rotation,
			// which doesn't need to be flipped because the change in handedness AND direction of the left x axis
			// causes a double negative - yeah, I know).
			
			pos[ 0 ] = -hapi_transform.position[ 0 ];
			pos[ 1 ] =  hapi_transform.position[ 1 ];
			pos[ 2 ] =  hapi_transform.position[ 2 ];
			
			Quaternion quat = new Quaternion( 	hapi_transform.rotationQuaternion[ 0 ],
												hapi_transform.rotationQuaternion[ 1 ],
												hapi_transform.rotationQuaternion[ 2 ],
												hapi_transform.rotationQuaternion[ 3 ] );
			
			Vector3 euler = quat.eulerAngles;
			euler.y = -euler.y;
			euler.z = -euler.z;
								
			quat = Quaternion.Euler( euler );
			
			Vector3 scale = new Vector3 ( hapi_transform.scale[ 0 ],
										  hapi_transform.scale[ 1 ],
										  hapi_transform.scale[ 2 ] );
			
			if( parent_object != null )
			{
				
				Matrix4x4 world_mat = Matrix4x4.identity;
				world_mat.SetTRS( pos, quat, scale );					
				Matrix4x4 local_mat = parent_xform_inverse  * world_mat;					
				
				quat = HAPI_AssetUtility.getQuaternion( local_mat );
				scale = HAPI_AssetUtility.getScale( local_mat );
				pos = HAPI_AssetUtility.getPosition( local_mat );
			}
			
			HAPI_CurvesCollection curves = myCurveCollection;						
			
			addKeyToCurve( curr_time, pos[0], curves.tx );
			addKeyToCurve( curr_time, pos[1], curves.ty );
			addKeyToCurve( curr_time, pos[2], curves.tz );
			addKeyToCurve( curr_time, quat.x, curves.qx );
			addKeyToCurve( curr_time, quat.y, curves.qy );
			addKeyToCurve( curr_time, quat.z, curves.qz );
			addKeyToCurve( curr_time, quat.w, curves.qw );
			addKeyToCurve( curr_time, scale.x, curves.sx );
			addKeyToCurve( curr_time, scale.y, curves.sy );
			addKeyToCurve( curr_time, scale.z, curves.sz );
			
			
		}
		catch ( HAPI_Error error )
		{
			Debug.LogWarning( error.ToString() );
			return;
		}
	}
	
	public void endBakeAnimation()
	{
		try
		{						
						
			HAPI_CurvesCollection curves = myCurveCollection;
			
			Animation anim_component = gameObject.GetComponent< Animation >();
			if( anim_component == null )
			{
				gameObject.AddComponent< Animation >();
				anim_component = gameObject.GetComponent< Animation >();
			}
			AnimationClip clip = new AnimationClip();
			anim_component.clip = clip;																		
			
			clip.SetCurve( "", typeof(Transform), "localPosition.x", curves.tx );				
			clip.SetCurve( "", typeof(Transform), "localPosition.y", curves.ty );				
			clip.SetCurve( "", typeof(Transform), "localPosition.z", curves.tz );							
			clip.SetCurve( "", typeof(Transform), "localRotation.x", curves.qx );				
			clip.SetCurve( "", typeof(Transform), "localRotation.y", curves.qy );				
			clip.SetCurve( "", typeof(Transform), "localRotation.z", curves.qz );
			clip.SetCurve( "", typeof(Transform), "localRotation.w", curves.qw );								
			clip.SetCurve( "", typeof(Transform), "localScale.x", curves.sx );				
			clip.SetCurve( "", typeof(Transform), "localScale.y", curves.sy );				
			clip.SetCurve( "", typeof(Transform), "localScale.z", curves.sz );

			clip.EnsureQuaternionContinuity();
			
			/*if( parent_object != null )
			{
				transform.parent = parent_object.transform;									
			}*/
						
		}
		catch ( HAPI_Error error )
		{
			Debug.LogWarning( error.ToString() );
			return;
		}
	}

	/////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	// Serialized Private Data

	[SerializeField] private int		myObjectId;
	[SerializeField] private string		myObjectName;
	[SerializeField] private bool		myObjectVisible;
	
	private HAPI_CurvesCollection myCurveCollection = null;
}
