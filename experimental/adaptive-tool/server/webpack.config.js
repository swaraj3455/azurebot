/* eslint-disable @typescript-eslint/no-var-requires */
  
/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

//@ts-check

'use strict';

const withDefaults = require('../shared.webpack.config');
const path = require('path');

module.exports = withDefaults({
	context: path.join(__dirname),
	entry: {
		extension: './lg/src/server.ts',
	},
	output: {
		filename: 'server.js',
		path: path.join(__dirname, 'out')
	}
});