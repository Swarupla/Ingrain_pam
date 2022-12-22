import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { TxtPreprocessComponent } from './txt-preprocess.component';

describe('TxtPreprocessComponent', () => {
  let component: TxtPreprocessComponent;
  let fixture: ComponentFixture<TxtPreprocessComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ TxtPreprocessComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(TxtPreprocessComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
