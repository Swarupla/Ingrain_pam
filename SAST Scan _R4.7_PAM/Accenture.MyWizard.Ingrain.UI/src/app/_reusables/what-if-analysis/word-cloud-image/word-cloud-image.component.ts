import { Component, OnInit, Input } from '@angular/core';
import { DomSanitizer } from '@angular/platform-browser';

@Component({
  selector: 'app-word-cloud-image',
  templateUrl: './word-cloud-image.component.html',
  styleUrls: ['./word-cloud-image.component.scss']
})
export class WordCloudImageComponent implements OnInit {

  private _WordCloudImageSanitized;

  @Input()
  set wordCloudImage(wordCloudImage: string) {
    const image = wordCloudImage.substring(2).slice(0, -1);
    this._WordCloudImageSanitized = this.domSanitizer.bypassSecurityTrustResourceUrl(
      'data:image/png;base64,' + image);

  }

  get wordCloudImage() {
    return this._WordCloudImageSanitized;
  }
  constructor(private domSanitizer: DomSanitizer) { }

  ngOnInit() {

  }

}
