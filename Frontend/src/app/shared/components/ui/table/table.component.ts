import { ChangeDetectionStrategy, Component } from '@angular/core';

@Component({
  selector: 'rm-table',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="overflow-x-auto">
      <table class="rm-table">
        <ng-content></ng-content>
      </table>
    </div>
  `
})
export class RmTableComponent {}
