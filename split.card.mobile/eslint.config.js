const path = require('path');
const { FlatCompat } = require('@eslint/eslintrc');

const compat = new FlatCompat({
  baseDirectory: __dirname,
  resolvePluginsRelativeTo: path.dirname(require.resolve('eslint-config-expo/package.json')),
});

module.exports = [
  { ignores: ['dist/*', '.expo/*', 'eslint.config.js'] },
  ...compat.extends('expo'),
];
