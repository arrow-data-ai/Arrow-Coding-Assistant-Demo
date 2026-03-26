// Help functionality for opening help pages in new tabs
window.openHelpPage = function(url) {
    // Validate that the URL is a help page path
    if (!url || typeof url !== 'string' || !url.startsWith('/help/')) {
        console.error('Invalid help page URL:', url);
        return;
    }
    
    // Open with tabnabbing protection using noopener
    const newWindow = window.open(url, '_blank', 'noopener');
    if (newWindow) {
        newWindow.opener = null; // Defensive programming - redundant but ensures protection
    }
};
