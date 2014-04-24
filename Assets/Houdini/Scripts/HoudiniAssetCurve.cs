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
using System.Runtime.InteropServices;
using System.Collections;
using System.Collections.Generic;

[ ExecuteInEditMode ]
public class HoudiniAssetCurve : HoudiniAsset
{
	/////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	// Public Properties
	
	// Please keep these in the same order and grouping as their initializations in HAPI_Asset.reset().

	public HoudiniCurve				prCurve {	get { 
													HoudiniCurve curve = gameObject.GetComponent< HoudiniCurve >();
													if ( curve == null )
													{
														curve = gameObject.AddComponent< HoudiniCurve >();
														curve.prControl = this;
														curve.prParms = this.prParms;
													}
													return curve;
												}
												private set { } }
	
	/////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	// Public Methods
	
	public HoudiniAssetCurve() 
	{
		if ( prEnableLogging )
			Debug.Log( "HoudiniAssetCurve created!" );

		reset();
	}
	
	~HoudiniAssetCurve()
	{}

	public override void reset()
	{
		base.reset();
		
		// Overwrite some settings that should be different by default for curves than other asset types.
		prAutoSelectAssetRootNode	= true;
		prHideGeometryOnLinking		= false;
		prAssetType					= AssetType.TYPE_CURVE;
	}

	public override void OnEnable()
	{
		// If this curve asset is a duplicate then it will have the same mesh
		// filter and mesh renderer as the curve it was duplicated from. Since
		// we want it to have its own, delete the existing ones here and new
		// ones will be created in the build.
		if ( isDuplicatingAsset() )
		{
			MeshFilter mesh_filter = gameObject.GetComponent< MeshFilter >();
			if ( mesh_filter != null )
				DestroyImmediate( mesh_filter );

			MeshRenderer mesh_renderer = gameObject.GetComponent< MeshRenderer >();
			if ( mesh_renderer != null )
				DestroyImmediate( mesh_renderer );
		}

		base.OnEnable();
	}
	
	public override bool build( bool reload_asset, bool unload_asset_first,
								bool serialization_recovery_only,
								bool force_reconnect,
								bool cook_downstream_assets,
								bool use_delay_for_progress_bar ) 
	{
		unload_asset_first = unload_asset_first && ( !serialization_recovery_only || isPrefab() );

		bool base_built = base.build( reload_asset, unload_asset_first, serialization_recovery_only, 
									  force_reconnect, cook_downstream_assets, use_delay_for_progress_bar );
		if ( !base_built )
			return false;
		
		return true;
	}

	/////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	// Protected Methods

	protected override int buildCreateAsset()
	{
		return HoudiniHost.createCurve();
	}

	protected override void buildFullBuildCustomWork( ref HoudiniProgressBar progress_bar )
	{
		prCurve.initDefaultParameters();
	}

	protected override void buildCreateObjects( bool reload_asset, ref HoudiniProgressBar progress_bar )
	{
		try
		{
			prCurve.syncPointsWithParm();
			prCurve.createObject( 0, 0 );
			HoudiniHost.repaint();
		}
		catch ( HoudiniError )
		{
			// Per-object errors are not re-thrown so that the rest of the asset has a chance to load.
			//Debug.LogWarning( error.ToString() );
		}
	}

}