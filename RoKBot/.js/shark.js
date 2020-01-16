// declare namespace
const shark = {};

shark.services = new (function () {
    
    var services = [];

    var has = this.has = function (name)
    {
        return (name == null || name.trim().length == 0 || typeof services[name] == "undefined") ? false : true;
    }

    this.register = function (name, handler)
    {
        if (name == null
		|| name.trim().length == 0
		|| typeof handler != "function"
		|| typeof services[name] != "undefined") return false;

        services[name.trim()] = handler;

        return true;
    }

    this.process = function (name, data)
    {
        return has(name) ? services[name.trim()](data) : null;
    }
});