using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace SmtpClient {
    
    public static class DragEventHelper {

        public static DragEventHandler CreateDragOverEventHandler(
            IEnumerable<string> supportedFormats,
            DragDropEffects effectWhenSupported = DragDropEffects.Copy,
            bool handledWhenNotSupported = true
        ) {
            return (sender, e) => {
                foreach (var format in supportedFormats) {
                    if (e.Data.GetDataPresent(format)) {
                        e.Effects = effectWhenSupported;
                        e.Handled = true;
                        return;
                    }
                }
                e.Effects = DragDropEffects.None;
                e.Handled = handledWhenNotSupported;
            };
        }
    }
}
