import { Component, Input, OnInit } from '@angular/core';

@Component({
  selector: 'app-loader-display',
  templateUrl: './loader-display.component.html',
  styleUrls: ['./loader-display.component.scss']
})
export class LoaderDisplayComponent implements OnInit {
  @Input() className;
  constructor() { }

  ngOnInit() {
  }
}