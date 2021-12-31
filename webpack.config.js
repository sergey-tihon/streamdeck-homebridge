// Note this only includes basic configuration for development mode.
// For a more comprehensive configuration check:
// https://github.com/fable-compiler/webpack-config-template

var path = require("path");

module.exports = {
    devtool: "source-map",
    mode: "development",
    entry: "./src/StreamDeck.Homebridge/App.fs.js",
    output: {
        path: path.join(__dirname, "./bin/com.sergeytihon.homebridge.sdPlugin"),
        filename: "bundle.js",
        library: {
            name: 'SDPlugin',
            type: 'var'
        },
    },
    devServer: {
        publicPath: "/",
        contentBase: "./bin/com.sergeytihon.homebridge.sdPlugin",
        port: 8080,
    },
    module: {
    }
}
