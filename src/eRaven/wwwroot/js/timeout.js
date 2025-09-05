function timeOutCall(dotnethelper) {
    document.onmousemove = resetTimeDelay;
    document.onkeypress = resetTimeDelay;

    function resetTimeDelay() {
        dotnethelper.invokeMethodAsync("TimerInterval");
    }
}