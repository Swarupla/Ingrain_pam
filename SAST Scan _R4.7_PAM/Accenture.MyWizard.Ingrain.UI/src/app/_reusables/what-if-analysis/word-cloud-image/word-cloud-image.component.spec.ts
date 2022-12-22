import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { WordCloudImageComponent } from './word-cloud-image.component';

describe('WordCloudImageComponent', () => {
  let component: WordCloudImageComponent;
  let fixture: ComponentFixture<WordCloudImageComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ WordCloudImageComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(WordCloudImageComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
