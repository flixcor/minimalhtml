// postcss.config.js
const postcssJitProps = require('postcss-jit-props');
const postcssCustomMedia = require('postcss-custom-media')
const path = require('path');

module.exports = {
  plugins: [
    postcssCustomMedia(),
    postcssJitProps({
      files: [
        path.resolve(__dirname, 'node_modules/open-props/open-props.min.css'),
      ]
    }),
  ]
}